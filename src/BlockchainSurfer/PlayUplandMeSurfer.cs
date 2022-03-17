using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Upland.Infrastructure.UplandApi;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.BlockchainTypes;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.BlockchainSurfer
{
    public class PlayUplandMeSurfer : IPlayUplandMeSurfer
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IBlockchainManager _blockchainManager;
        private readonly IUplandApiManager _uplandApiManager;
        private readonly string _playuplandme;

        private List<string> _propertyIdsToWatch;
        List<Tuple<decimal, string, string>> _registeredUserEOSAccounts;

        private List<Neighborhood> _neighborhoods;
        private bool _isProcessing;

        public PlayUplandMeSurfer(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager, IBlockchainManager blockchainManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;
            _blockchainManager = blockchainManager;
            _playuplandme = "playuplandme";

            _neighborhoods = new List<Neighborhood>();
            _isProcessing = false;
        }

        public async Task RunBlockChainUpdate()
        {
            long lastActionProcessed = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUPLANDACTIONSEQNUM));

            try
            {
                await ProcessBlockchainFromAction(lastActionProcessed);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - RunBlockChainUpdate", ex.Message);
                _isProcessing = false;
            }
        }

        public async Task BuildBlockChainFromBegining()
        {
            // Upland went live on the blockchain on 2019-06-06 11:51:37
            DateTime startDate = new DateTime(2019, 06, 05, 01, 00, 00);

            await ProcessBlockchainFromAction(-1);
        }

        public async Task ProcessBlockchainFromAction(long lastActionProcessed)
        {
            bool enableUpdates = bool.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES));

            if (!enableUpdates || _isProcessing)
            {
                return;
            }

            _isProcessing = true;

            DateTime historyTimeStamp = _localDataManager.GetLastHistoricalCityStatusDate();
            _registeredUserEOSAccounts = _localDataManager.GetRegisteredUsersEOSAccounts();
            _propertyIdsToWatch = _localDataManager.GetConfigurationValue(Consts.CONFIG_PROPIDSTOMONITORFORSENDS).Split(",").ToList();

            if (historyTimeStamp == DateTime.MinValue)
            {
                historyTimeStamp = new DateTime(2019, 06, 05);
            }
            else
            {
                historyTimeStamp = new DateTime(historyTimeStamp.Year, historyTimeStamp.Month, historyTimeStamp.Day);
            }

            bool continueLoad = true;

            while (continueLoad)
            {
                List<PlayUplandMeAction> actions = new List<PlayUplandMeAction>();

                // Have to do this in a retry loop due to timeouts
                bool retry = true;
                while (retry)
                {
                    try
                    {
                        Thread.Sleep(2000);
                        GetPlayUplandMeActionsResponse response = await _blockchainManager.GetEOSFlareActions<GetPlayUplandMeActionsResponse>(lastActionProcessed + 1, _playuplandme);
                        
                        if (response != null && response.actions != null)
                        {
                            actions = response.actions;
                            retry = false;
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - BuildBlockChainFromDate - Loop", ex.Message);
                        Thread.Sleep(5000);
                    }
                }

                if (actions.Count == 0)
                {
                    //continueLoad = false;
                }
                else
                {
                    try
                    {
                        await ProcessActions(actions);
                    }
                    catch (Exception ex)
                    {
                        _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessActions - Exception Bubbled Up Disable Blockchain Updates", ex.Message);
                        _localDataManager.UpsertConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES, false.ToString());
                        continueLoad = false;
                    }

                    lastActionProcessed = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUPLANDACTIONSEQNUM));

                    if (historyTimeStamp.AddDays(1).Ticks < actions.Last().block_time.Ticks)
                    {
                        historyTimeStamp = new DateTime(actions.Last().block_time.Year, actions.Last().block_time.Month, actions.Last().block_time.Day);
                        _localDataManager.SetHistoricalCityStats(historyTimeStamp);
                    }
                }
            }

            _isProcessing = false;
        }

        private async Task ProcessActions(List<PlayUplandMeAction> actions)
        {
            long maxActionSeqNum = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUPLANDACTIONSEQNUM));
            foreach (PlayUplandMeAction action in actions)
            {
                if (action.account_action_seq < maxActionSeqNum)
                {
                    // We've already processed this event
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessActions", string.Format("Skipping Action {0} < {1}", action.account_action_seq, maxActionSeqNum));
                    continue;
                }

                switch (action.action_trace.act.name)
                {
                    case "a4":
                        await ProcessMintingAction(action);
                        break;
                    case "n12":
                        ProcessOfferAction(action);
                        break;
                    case "n13":
                        ProcessOfferResolutionAction(action);
                        break;
                    case "n5":
                        await ProcessPurchaseAction(action);
                        break;
                    case "n2":
                        ProcessPlaceForSaleAction(action);
                        break;
                    case "n4":
                        ProcessRemoveFromSaleAction(action);
                        break;
                    case "n52":
                        await ProcessBuyForFiatAction(action);
                        break;
                    case "n33":
                        ProcessBecomeUplanderAction(action);
                        break;
                    case "n34":
                        ProcessDeleteVisitorAction(action);
                        break;
                    case "n41":
                       // ProcessSendAction(action);
                        break;
                    case "n111":
                        //ProcessSendUPXAction(action);
                        break;
                }

                if (action.account_action_seq > maxActionSeqNum)
                {
                    maxActionSeqNum = action.account_action_seq;
                    _localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXUPLANDACTIONSEQNUM, action.account_action_seq.ToString());
                }
            }
        }

        private void ProcessDeleteVisitorAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.p52 == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessDeleteVisitorAction", string.Format("p52 (Visitor EOS): {0}, Trx_id: {1}", action.action_trace.act.data.p52, action.action_trace.trx_id));
                return;
            }

            List<Property> properties = _localDataManager.GetPropertiesByUplandUsername(action.action_trace.act.data.p52);
            _localDataManager.DeleteEOSUser(action.action_trace.act.data.p52);

            foreach (Property prop in properties)
            {
                prop.Owner = null;
                prop.Status = Consts.PROP_STATUS_UNLOCKED;
                prop.MintedBy = null;
                prop.MintedOn = null;
                _localDataManager.DeleteSaleHistoryByPropertyId(prop.Id);
                _localDataManager.UpsertProperty(prop);
            }
        }

        private void ProcessBecomeUplanderAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.p53 == null || action.action_trace.act.data.p52 == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessBecomeUplanderAction", string.Format("p53 (New EOS): {0}, p52 (Visitor EOS): {1}, Trx_id: {2}", action.action_trace.act.data.p53, action.action_trace.act.data.p52, action.action_trace.trx_id));
                return;
            }
            string uplandUsername = action.action_trace.act.data.memo.Split(" notarizes that Upland user ")[1].Split(" with corresponding ")[0];

            List<Property> properties = _localDataManager.GetPropertiesByUplandUsername(action.action_trace.act.data.p52);

            foreach (Property prop in properties)
            {
                prop.Owner = action.action_trace.act.data.p53;
                prop.MintedBy = action.action_trace.act.data.p53;
                _localDataManager.UpsertProperty(prop);
            }

            _localDataManager.DeleteEOSUser(action.action_trace.act.data.p52);

            _localDataManager.UpsertEOSUser(new EOSUser
            {
                EOSAccount = action.action_trace.act.data.p53,
                UplandUsername = uplandUsername,
                Joined = action.block_time,
                Spark = 0
            });
        }

        private async Task ProcessBuyForFiatAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.a45 == null || action.action_trace.act.data.p14 == null)
            {
                PlayUplandMeTransactionEntry transactionEntry = await _blockchainManager.GetSingleTransactionById<PlayUplandMeTransactionEntry>(action.action_trace.trx_id);

                if (transactionEntry.traces.Where(t => t.act.name == "n52").ToList().Count == 1)
                {
                    PlayUplandMeData traceData = transactionEntry.traces.Where(t => t.act.name == "n52").First().act.data;
                    action.action_trace.act.data.a45 = traceData.a45;
                    action.action_trace.act.data.p14 = traceData.p14;
                }
                else
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessBuyForFiatAction - Missing Data", string.Format("a45 (propId): {0}, p14 (Buyer EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.p14, action.action_trace.trx_id));
                    return;
                }
            }

            long propId = long.Parse(action.action_trace.act.data.a45);
            Property property = _localDataManager.GetProperty(propId);
            List<SaleHistoryEntry> allEntries = _localDataManager.GetRawSaleHistoryByPropertyId(propId);
            List<SaleHistoryEntry> buyEntries = allEntries
                .Where(e => e.BuyerEOS == null && !e.Offer && e.AmountFiat > 0)
                .OrderByDescending(e => e.DateTime).ToList();
            
            if (property.MintedBy == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessBuyForFiatAction - Never Minted", string.Format("a45 (propId): {0}, p14 (Buyer EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.p14, action.action_trace.trx_id));
                property.MintedBy = action.action_trace.act.data.p14;
                property.MintedOn = action.block_time;
            }

            if (property.Owner == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessBuyForFiatAction - Never Owned", string.Format("a45 (propId): {0}, p14 (Buyer EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.p14, action.action_trace.trx_id));
                property.Owner = action.action_trace.act.data.p14;
            }

            if (buyEntries.Count == 0)
            {
                // If for some reason the sale already got set, check to see if it is there
                buyEntries = allEntries
                    .Where(e => e.BuyerEOS == action.action_trace.act.data.p14 && !e.Offer && e.AmountFiat > 0)
                    .OrderByDescending(e => e.DateTime).ToList();

                if (buyEntries.Count > 0)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessBuyForFiatAction - Found Resolved Buy Action", string.Format("a45 (propId): {0}, p14 (Buyer EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.p14, action.action_trace.trx_id));
                }
            }

            if (buyEntries.Count > 0)
            {
                buyEntries.First().BuyerEOS = action.action_trace.act.data.p14;
                buyEntries.First().DateTime = action.block_time;
                _localDataManager.UpsertSaleHistory(buyEntries.First());

                foreach (SaleHistoryEntry entry in allEntries)
                {
                    if (entry.Id != buyEntries.First().Id && (entry.BuyerEOS == null || entry.SellerEOS == null))
                    {
                        _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }
            else
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessBuyForFiatAction - No Sale Entry Found", string.Format("a45 (propId): {0}, p14 (Buyer EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.p14, action.action_trace.trx_id));
                
                // Clear open sale entries anyway since it changed hands
                foreach (SaleHistoryEntry entry in allEntries)
                {
                    if (entry.BuyerEOS == null || entry.SellerEOS == null)
                    {
                        _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }

            // Dumb user has bout something before minting
            EOSUser existingAccount = _localDataManager.GetUplandUsernameByEOSAccount(action.action_trace.act.data.p14);
            if (existingAccount == null)
            {
                _localDataManager.UpsertEOSUser(new EOSUser
                {
                    EOSAccount = action.action_trace.act.data.p14,
                    UplandUsername = "",
                    Joined = action.block_time,
                    Spark = 0
                });
            }

            if (property != null && property.Address != null && property.Address != "")
            {
                property.Status = Consts.PROP_STATUS_OWNED;
                property.Owner = action.action_trace.act.data.p14;
                _localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessRemoveFromSaleAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.a45 == null || action.action_trace.act.data.a54 == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessRemoveFromSaleAction", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.trx_id));
                return;
            }

            long propId = long.Parse(action.action_trace.act.data.a45);
            List<SaleHistoryEntry> historyEntries = _localDataManager.GetRawSaleHistoryByPropertyId(propId);

            bool deletedHistoryEntry = false;
            foreach (SaleHistoryEntry entry in historyEntries)
            {
                if (entry.SellerEOS == action.action_trace.act.data.a54 && entry.BuyerEOS == null)
                {
                    _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    deletedHistoryEntry = true;
                }
            }

            if (!deletedHistoryEntry)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessRemoveFromSaleAction - No Active Sale Entry Found", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.trx_id));
            }

            Property property = _localDataManager.GetProperty(propId);

            if (property != null && property.Address != null && property.Address != "")
            {
                if (property.MintedBy == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessRemoveFromSaleAction - Never Minted", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.trx_id));
                    property.MintedBy = action.action_trace.act.data.a54;
                    property.MintedOn = action.block_time;
                }

                if (property.Owner == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessRemoveFromSaleAction - Never Owned", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, Trx_id: {2}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.trx_id));
                    property.Owner = action.action_trace.act.data.a54;
                }

                property.Status = Consts.PROP_STATUS_OWNED;
                _localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessPlaceForSaleAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.a45 == null || action.action_trace.act.data.a54 == null || action.action_trace.act.data.p11 == null || action.action_trace.act.data.p3 == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPlaceForSaleAction", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.act.data.p11, action.action_trace.act.data.p3, action.action_trace.trx_id));
                return;
            }

            SaleHistoryEntry historyEntry = new SaleHistoryEntry
            {
                DateTime = action.block_time,
                SellerEOS = action.action_trace.act.data.a54,
                BuyerEOS = null,
                PropId = long.Parse(action.action_trace.act.data.a45),
                Offer = false
            };

            List<SaleHistoryEntry> allEntries = _localDataManager.GetRawSaleHistoryByPropertyId(historyEntry.PropId);

            if (Regex.Match(action.action_trace.act.data.p11, "^0.00 UP").Success)
            {
                historyEntry.AmountFiat = double.Parse(action.action_trace.act.data.p3.Split(" FI")[0]);
                historyEntry.Amount = null;
            }
            else if (Regex.Match(action.action_trace.act.data.p3, "^0.00 FI").Success)
            {
                historyEntry.Amount = double.Parse(action.action_trace.act.data.p11.Split(" UP")[0]);
                historyEntry.AmountFiat = null;
            }
            else
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPlaceForSaleAction - Failed Parsing Amount", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.act.data.p11, action.action_trace.act.data.p3, action.action_trace.trx_id));
            }

            Property property = _localDataManager.GetProperty(historyEntry.PropId);

            if (property != null && property.Address != null && property.Address != "")
            {
                if (property.MintedBy == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPlaceForSaleAction - Property Never Minted", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.act.data.p11, action.action_trace.act.data.p3, action.action_trace.trx_id));
                    property.MintedBy = action.action_trace.act.data.a54;
                    property.MintedOn = action.block_time;
                }
                
                if (property.Owner == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPlaceForSaleAction - Owner Is Null", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.act.data.p11, action.action_trace.act.data.p3, action.action_trace.trx_id));
                    property.Owner = action.action_trace.act.data.a54;
                }
                
                if (property.Owner != historyEntry.SellerEOS)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPlaceForSaleAction - Blockchain Error Seller is not Owner", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.act.data.p11, action.action_trace.act.data.p3, action.action_trace.trx_id));
                    property.Owner = historyEntry.SellerEOS;
                }

                if (allEntries.Any(s => 
                    s.BuyerEOS == null && 
                    s.SellerEOS == historyEntry.SellerEOS && 
                    s.AmountFiat == historyEntry.AmountFiat &&
                    s.Amount == historyEntry.Amount))
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPlaceForSaleAction - Duplicate Sale Entry Found", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.act.data.p11, action.action_trace.act.data.p3, action.action_trace.trx_id));
                    property.Status = Consts.PROP_STATUS_FORSALE;
                    _localDataManager.UpsertProperty(property);
                    return;
                }

                _localDataManager.UpsertSaleHistory(historyEntry);
                property.Status = Consts.PROP_STATUS_FORSALE;
                _localDataManager.UpsertProperty(property);
            }
            else
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPlaceForSaleAction - Property Not Found", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.a54, action.action_trace.act.data.p11, action.action_trace.act.data.p3, action.action_trace.trx_id));
            }
        }

        private async Task ProcessPurchaseAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.p14 == null || action.action_trace.act.data.a45 == null || action.action_trace.act.data.p24 == null || action.action_trace.act.data.memo == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPurchaseAction", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.p14, action.action_trace.act.data.a45, action.action_trace.act.data.p24, action.action_trace.act.data.memo, action.action_trace.trx_id));
                return;
            }

            long propId = long.Parse(action.action_trace.act.data.a45);

            Property property = _localDataManager.GetProperty(propId);
            if (property == null || property.Address == null || property.Address == "")
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPurchaseAction - Missing Prop", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.p14, action.action_trace.act.data.a45, action.action_trace.act.data.p24, action.action_trace.act.data.memo, action.action_trace.trx_id));
                return;
            }

            List<SaleHistoryEntry> allEntries = _localDataManager.GetRawSaleHistoryByPropertyId(propId);
            List<SaleHistoryEntry> buyEntries = allEntries
                .Where(e => e.BuyerEOS == null && !e.Offer && e.Amount == double.Parse(action.action_trace.act.data.p24.Split(" UP")[0]))
                .OrderByDescending(e => e.DateTime).ToList();

            if (property.MintedBy == null)
            {
                string ogMinttx = action.action_trace.act.data.memo.Split("transaction: ")[1];

                PlayUplandMeTransactionEntry mintTransaction = await _blockchainManager.GetSingleTransactionById<PlayUplandMeTransactionEntry>(action.action_trace.act.data.memo.Split("transaction: ")[1]);

                if (mintTransaction == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPurchaseAction - Never Minted", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.p14, action.action_trace.act.data.a45, action.action_trace.act.data.p24, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    property.MintedBy = action.action_trace.act.data.p14;
                    property.MintedOn = action.block_time;
                }
                else
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPurchaseAction - Never Minted - Got Mint Transaction", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.p14, action.action_trace.act.data.a45, action.action_trace.act.data.p24, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    property.MintedBy = mintTransaction.traces[0].act.data.a54;
                    property.MintedOn = mintTransaction.block_time;
                }
            }

            if (property.Owner == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPurchaseAction - Never Owned", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.p14, action.action_trace.act.data.a45, action.action_trace.act.data.p24, action.action_trace.act.data.memo, action.action_trace.trx_id));
                property.Owner = action.action_trace.act.data.p14;
            }

            if (buyEntries.Count == 0)
            {
                if (property.Owner == action.action_trace.act.data.p14)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPurchaseAction - Blockchain Fault Owner == Buyer", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.p14, action.action_trace.act.data.a45, action.action_trace.act.data.p24, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    return;
                }

                if (!allEntries.Any(b =>
                         b.SellerEOS == property.Owner &&
                         b.BuyerEOS == action.action_trace.act.data.p14 &&
                         b.PropId == property.Id &&
                         !b.Offer &&
                         !b.Accepted &&
                         b.AmountFiat == null &&
                         b.Amount > 0
                    ))
                {
                    _localDataManager.UpsertSaleHistory(
                        new SaleHistoryEntry
                        {
                            DateTime = action.block_time,
                            SellerEOS = property.Owner,
                            BuyerEOS = action.action_trace.act.data.p14,
                            PropId = propId,
                            Amount = double.Parse(action.action_trace.act.data.p24.Split(" UP")[0]),
                            AmountFiat = null,
                            OfferPropId = null,
                            Offer = false,
                            Accepted = false
                        }
                    );

                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPurchaseAction - Added Missing Buy Entry", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.p14, action.action_trace.act.data.a45, action.action_trace.act.data.p24, action.action_trace.act.data.memo, action.action_trace.trx_id));
                }
                else
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessPurchaseAction - Could Not Find Buy Entry", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.p14, action.action_trace.act.data.a45, action.action_trace.act.data.p24, action.action_trace.act.data.memo, action.action_trace.trx_id));
                }
            }
            else
            {
                buyEntries.First().BuyerEOS = action.action_trace.act.data.p14;
                buyEntries.First().DateTime = action.block_time;
                _localDataManager.UpsertSaleHistory(buyEntries.First());
            }

            foreach (SaleHistoryEntry entry in allEntries)
            {
                if (entry.BuyerEOS == null || entry.SellerEOS == null)
                {
                    _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                }
            }

            // Dumb user has bout something before minting
            EOSUser existingAccount = _localDataManager.GetUplandUsernameByEOSAccount(action.action_trace.act.data.p14);
            if (existingAccount == null)
            {
                _localDataManager.UpsertEOSUser(new EOSUser
                {
                    EOSAccount = action.action_trace.act.data.p14,
                    UplandUsername = action.action_trace.act.data.memo.Split(" that Upland user ")[1].Split(" with EOS account ")[0],
                    Joined = action.block_time,
                    Spark = 0
                });
            }

            property.Status = Consts.PROP_STATUS_OWNED;
            property.Owner = action.action_trace.act.data.p14;
            _localDataManager.UpsertProperty(property);
        }

        private void ProcessOfferResolutionAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.p25 == null || action.action_trace.act.data.memo == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                return;
            }

            if (Regex.Match(action.action_trace.act.data.memo, @"\) and Upland user ").Success)
            {
                int propOneCityId = 1;
                int propTwoCityId = 1;
                string propOneAddress = "";
                string propTwoAddress = "";

                if (Regex.Match(action.action_trace.act.data.memo, "^[^,]+,[^,]+,[^,]+,[^,]+,[^,]+$").Success)
                {
                    propOneCityId = HelperFunctions.HelperFunctions.GetCityIdByName(action.action_trace.act.data.memo.Split(", ")[1]);
                    propOneAddress = action.action_trace.act.data.memo.Split(" owns ")[1].Split(", ")[0];

                    propTwoCityId = HelperFunctions.HelperFunctions.GetCityIdByName(action.action_trace.act.data.memo.Split(", ")[3]);
                    propTwoAddress = action.action_trace.act.data.memo.Split(" owns ")[2].Split(", ")[0];
                }
                else if (Regex.Match(action.action_trace.act.data.memo, "^[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+$").Success || Regex.Match(action.action_trace.act.data.memo, "^[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+,[^,]+$").Success)
                {
                    // First match 3 commas
                    if (Regex.Match(action.action_trace.act.data.memo.Split(")")[1], "^[^,]+,[^,]+,[^,]+,[^,]+$").Success)
                    {
                        string cityName = action.action_trace.act.data.memo.Split(")")[1].Split(",")[2].Trim();
                        propOneCityId = HelperFunctions.HelperFunctions.GetCityIdByName(cityName);
                        propOneAddress = action.action_trace.act.data.memo.Split(")")[1].Split(" owns ")[1].Split(string.Format(", {0}", cityName))[0];
                    }
                    else
                    {
                        propOneCityId = HelperFunctions.HelperFunctions.GetCityIdByName(action.action_trace.act.data.memo.Split(")")[1].Split(",")[1].Trim());
                        propOneAddress = action.action_trace.act.data.memo.Split(" owns ")[1].Split(",")[0].Trim();
                    }

                    if (Regex.Match(action.action_trace.act.data.memo.Split(")")[3], "^[^,]+,[^,]+,[^,]+,[^,]+$").Success)
                    {
                        string cityName = action.action_trace.act.data.memo.Split(")")[3].Split(",")[2].Trim();
                        propTwoCityId = HelperFunctions.HelperFunctions.GetCityIdByName(cityName);
                        propTwoAddress = action.action_trace.act.data.memo.Split(")")[3].Split(" owns ")[1].Split(string.Format(", {0}", cityName))[0];
                    }
                    else
                    {
                        propTwoCityId = HelperFunctions.HelperFunctions.GetCityIdByName(action.action_trace.act.data.memo.Split(")")[3].Split(",")[1].Trim());
                        propTwoAddress = action.action_trace.act.data.memo.Split(" owns ")[2].Split(",")[0].Trim();
                    }
                }
                else
                {
                    propOneAddress = action.action_trace.act.data.memo.Split(" owns ")[1].Split(" (ini")[0];
                    propTwoAddress = action.action_trace.act.data.memo.Split(" owns ")[2].Split(" (ini")[0];
                }

                Property propOne = _localDataManager.GetPropertyByCityIdAndAddress(propOneCityId, propOneAddress);
                Property propTwo = _localDataManager.GetPropertyByCityIdAndAddress(propTwoCityId, propTwoAddress);

                propOne.Owner = action.action_trace.act.data.memo.Split("(EOS account ")[1].Split(") owns")[0];
                propOne.Status = Consts.PROP_STATUS_OWNED;

                if (propOne.MintedBy == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - SWAP ONE Never Minted", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    propOne.MintedBy = propOne.Owner;
                    propOne.MintedOn = action.block_time;
                }

                if (propOne != null && propOne.Address != null && propOne.Address != "")
                {
                    _localDataManager.UpsertProperty(propOne);
                }
                else
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - Could Not Find Prop (SWAP)", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                }

                propTwo.Owner = action.action_trace.act.data.memo.Split("(EOS account ")[2].Split(") now owns ")[0];
                propTwo.Status = Consts.PROP_STATUS_OWNED;

                if (propTwo.MintedBy == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - SWAP TWO Never Minted", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    propTwo.MintedBy = propOne.Owner;
                    propTwo.MintedOn = action.block_time;
                }

                if (propTwo != null && propTwo.Address != null && propTwo.Address != "")
                {
                    _localDataManager.UpsertProperty(propTwo);
                }
                else
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - Could Not Find Prop (SWAP)", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                }

                List<SaleHistoryEntry> allEntriesOne = _localDataManager.GetRawSaleHistoryByPropertyId(propOne.Id);
                List<SaleHistoryEntry> allEntriesTwo = _localDataManager.GetRawSaleHistoryByPropertyId(propTwo.Id);

                SaleHistoryEntry buyEntry = allEntriesTwo
                    .Where(e => e.SellerEOS == null && e.Offer && e.BuyerEOS == action.action_trace.act.data.memo.Split("(EOS account ")[2].Split(") now owns ")[0])
                    .OrderByDescending(e => e.DateTime)
                    .FirstOrDefault();

                if (buyEntry != null)
                {
                    buyEntry.SellerEOS = action.action_trace.act.data.memo.Split("(EOS account ")[1].Split(") owns")[0];
                    buyEntry.Accepted = true;
                    buyEntry.DateTime = action.block_time;
                    _localDataManager.UpsertSaleHistory(buyEntry);
                }
                else
                {
                    if (!allEntriesTwo.Any(b =>
                         b.SellerEOS == action.action_trace.act.data.memo.Split("(EOS account ")[1].Split(") owns")[0] &&
                         b.BuyerEOS == action.action_trace.act.data.memo.Split("(EOS account ")[2].Split(") now owns ")[0] &&
                         b.PropId == propTwo.Id &&
                         b.Offer &&
                         b.Accepted &&
                         b.AmountFiat == null &&
                         b.Amount == null &&
                         b.OfferPropId == propOne.Id
                    ))
                    {
                        // buy offer entry is null, lets create one
                        _localDataManager.UpsertSaleHistory(new SaleHistoryEntry
                        {
                            DateTime = action.block_time,
                            SellerEOS = action.action_trace.act.data.memo.Split("(EOS account ")[1].Split(") owns")[0],
                            BuyerEOS = action.action_trace.act.data.memo.Split("(EOS account ")[2].Split(") now owns ")[0],
                            PropId = propTwo.Id,
                            Amount = null,
                            AmountFiat = null,
                            OfferPropId = propOne.Id,
                            Offer = true,
                            Accepted = true
                        });

                        _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - SWAP Did Not Find Completed Offer, Created One", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    }
                    else
                    {
                        _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - SWAP Found Completed Offer Resolution)", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    }
                }

                foreach (SaleHistoryEntry entry in allEntriesOne)
                {
                    if (entry.BuyerEOS == null || entry.SellerEOS == null)
                    {
                        _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }

                foreach (SaleHistoryEntry entry in allEntriesTwo)
                {
                    if (entry.BuyerEOS == null || entry.SellerEOS == null)
                    {
                        _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }
            else
            {
                string cityName = HelperFunctions.HelperFunctions.SusOutCityNameByMemoString(action.action_trace.act.data.memo);
                Property prop = _localDataManager.GetPropertyByCityIdAndAddress(
                    HelperFunctions.HelperFunctions.GetCityIdByName(cityName),
                    action.action_trace.act.data.memo.Split(" owns ")[1].Split(string.Format(", {0}", cityName))[0]);

                if (prop == null || prop.Id == 0 || prop.Address == null || prop.Address == "")
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - Could Not Find Prop", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    return;
                }

                string newOwner = action.action_trace.act.data.memo.Split("EOS account ")[1].Split(" owns ")[0];
                prop.Owner = newOwner;
                prop.Status = Consts.PROP_STATUS_OWNED;

                if (prop.MintedBy == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - Never Minted", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    prop.MintedBy = newOwner;
                    prop.MintedOn = action.block_time;
                }

                if (prop.Owner == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - Never Owned", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    prop.Owner = newOwner;
                }

                _localDataManager.UpsertProperty(prop);

                // Dumb user has bout something before minting
                EOSUser existingAccount = _localDataManager.GetUplandUsernameByEOSAccount(newOwner);
                if (existingAccount == null)
                {
                    _localDataManager.UpsertEOSUser(new EOSUser
                    {
                        EOSAccount = newOwner,
                        UplandUsername = action.action_trace.act.data.memo.Split(" that Upland user ")[1].Split(" with EOS account")[0],
                        Joined = action.block_time,
                        Spark = 0
                    });
                }
                else if (existingAccount.UplandUsername == "")
                {
                    existingAccount.UplandUsername = action.action_trace.act.data.memo.Split(" that Upland user ")[1].Split(" with EOS account")[0];
                    _localDataManager.UpsertEOSUser(existingAccount);
                }

                List<SaleHistoryEntry> allEntries = _localDataManager.GetRawSaleHistoryByPropertyId(prop.Id);
                SaleHistoryEntry buyEntry = allEntries
                    .Where(e => e.SellerEOS == null && e.Offer && e.BuyerEOS == newOwner)
                    .OrderByDescending(e => e.DateTime)
                    .FirstOrDefault();

                if (action.action_trace.act.data.p25 == newOwner)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - Blockchain Error Buyer = Seller", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                }
                else
                {
                    if (buyEntry != null)
                    {
                        buyEntry.SellerEOS = action.action_trace.act.data.p25;
                        buyEntry.Accepted = true;
                        buyEntry.DateTime = action.block_time;
                        _localDataManager.UpsertSaleHistory(buyEntry);
                    }
                    else
                    {
                        if (!allEntries.Any(b =>
                             b.SellerEOS == action.action_trace.act.data.p25 &&
                             b.BuyerEOS == newOwner &&
                             b.PropId == prop.Id &&
                             b.Offer &&
                             b.Accepted
                        ))
                        {
                            _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - Could Not Find Completed Offer Entry", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                        }
                        else
                        {
                            _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferResolutionAction - Found Completed Offer Entry", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.action_trace.act.data.p25, action.action_trace.act.data.memo, action.action_trace.trx_id));
                        }
                    }
                }

                foreach (SaleHistoryEntry entry in allEntries)
                {
                    if (entry.BuyerEOS == null || entry.SellerEOS == null)
                    {
                        _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }
        }

        private void ProcessOfferAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.p23 == null || action.action_trace.act.data.p15 == null || action.action_trace.act.data.p21 == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferAction", string.Format("p23 (Buyer EOS): {0}, p15 (PropId): {1}, p21 (Offer): {2}, Trx_id: {3}", action.action_trace.act.data.p23, action.action_trace.act.data.p15, string.Join(" ", action.action_trace.act.data.p21), action.action_trace.trx_id));
                return;
            }

            SaleHistoryEntry historyEntry = new SaleHistoryEntry
            {
                DateTime = action.block_time,
                BuyerEOS = action.action_trace.act.data.p23,
                SellerEOS = null,
                PropId = long.Parse(action.action_trace.act.data.p15),
                Offer = true
            };

            if (action.action_trace.act.data.p21[0] == "asset")
            {
                historyEntry.Amount = double.Parse(action.action_trace.act.data.p21[1].Split(" UP")[0]);
                historyEntry.AmountFiat = null;
            }
            else if (action.action_trace.act.data.p21[0] == "uint64")
            {
                historyEntry.OfferPropId = long.Parse(action.action_trace.act.data.p21[1]);
                historyEntry.AmountFiat = null;
                historyEntry.Amount = null;
            }
            else
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferAction - Could Not Parse p21", string.Format("p23 (Buyer EOS): {0}, p15 (PropId): {1}, p21 (Offer): {2}, Trx_id: {3}", action.action_trace.act.data.p23, action.action_trace.act.data.p15, string.Join(" ", action.action_trace.act.data.p21), action.action_trace.trx_id));
            }

            Property property = _localDataManager.GetProperty(long.Parse(action.action_trace.act.data.p15));

            if (property != null)
            {
                if (property.MintedBy == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferAction - Never Minted", string.Format("p23 (Buyer EOS): {0}, p15 (PropId): {1}, p21 (Offer): {2}, Trx_id: {3}", action.action_trace.act.data.p23, action.action_trace.act.data.p15, string.Join(" ", action.action_trace.act.data.p21), action.action_trace.trx_id));
                }

                if (property.Owner == null)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferAction - Never Owned", string.Format("p23 (Buyer EOS): {0}, p15 (PropId): {1}, p21 (Offer): {2}, Trx_id: {3}", action.action_trace.act.data.p23, action.action_trace.act.data.p15, string.Join(" ", action.action_trace.act.data.p21), action.action_trace.trx_id));
                }
                _localDataManager.UpsertSaleHistory(historyEntry);
            }
            else
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessOfferAction - Property Doesn't Exist", string.Format("p23 (Buyer EOS): {0}, p15 (PropId): {1}, p21 (Offer): {2}, Trx_id: {3}", action.action_trace.act.data.p23, action.action_trace.act.data.p15, string.Join(" ", action.action_trace.act.data.p21), action.action_trace.trx_id));
            }
        }

        private async Task ProcessMintingAction(PlayUplandMeAction action)
        {
            if (action.action_trace.act.data.a45 == null || action.action_trace.act.data.p44 == null || action.action_trace.act.data.a54 == null || action.action_trace.act.data.memo == null)
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessMintingAction", string.Format("a45 (PropId): {0}, p44 (Amount): {1}, a54 (Minter EOS): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.p44, action.action_trace.act.data.a54, action.action_trace.act.data.memo, action.action_trace.trx_id));
                return;
            }

            string uplandUsername = action.action_trace.act.data.memo.Split(" notarizes that Upland user ")[1].Split(" with corresponding EOS account ")[0];
            EOSUser existingAccount = _localDataManager.GetUplandUsernameByEOSAccount(action.action_trace.act.data.a54);

            if (existingAccount != null && existingAccount.UplandUsername != uplandUsername)
            {
                List<Property> properties = _localDataManager.GetPropertiesByUplandUsername(existingAccount.UplandUsername);
                if (properties.Count != 0)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessMintingAction - Existing Old EOS Has Props", string.Format("Existing Uplander {5}, a45 (PropId): {0}, p44 (Amount): {1}, a54 (Minter EOS): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.p44, action.action_trace.act.data.a54, action.action_trace.act.data.memo, action.action_trace.trx_id, existingAccount.UplandUsername));

                    foreach (Property prop in properties)
                    {
                        prop.Owner = null;
                        prop.Status = Consts.PROP_STATUS_UNLOCKED;
                        prop.MintedBy = null;
                        prop.MintedOn = null;
                        _localDataManager.DeleteSaleHistoryByPropertyId(prop.Id);
                        _localDataManager.UpsertProperty(prop);
                    }

                }

                _localDataManager.DeleteEOSUser(existingAccount.EOSAccount);
                _localDataManager.UpsertEOSUser(new EOSUser
                {
                    EOSAccount = action.action_trace.act.data.a54,
                    UplandUsername = uplandUsername,
                    Joined = action.block_time,
                    Spark = 0
                });
            }
            
            if (existingAccount == null)
            {
                _localDataManager.UpsertEOSUser(new EOSUser
                {
                    EOSAccount = action.action_trace.act.data.a54,
                    UplandUsername = uplandUsername,
                    Joined = action.block_time,
                    Spark = 0
                });
            }

            if (existingAccount != null && existingAccount.UplandUsername == "")
            {
                existingAccount.UplandUsername = uplandUsername;
                _localDataManager.UpsertEOSUser(existingAccount);
            }    

            Property property = _localDataManager.GetProperty(long.Parse(action.action_trace.act.data.a45));

            // We might be missing this property in the database
            if (property == null || property.Address == null || property.Address == "")
            {
                property = await TryToLoadPropertyById(long.Parse(action.action_trace.act.data.a45));
            }

            if (property != null && property.Address != null && property.Address != "")
            {
                // if the Mint is 0, then double check and set the mint price
                if (property.Mint == 0)
                {
                    UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(property.Id);
                    Property prop = UplandMapper.Map(uplandProperty);

                    property.Mint = prop.Mint;

                    // DEBUG REMOVE IF DETROIT FAILS
                    //SetMintsOnRestOfNeighborhood(property);
                }

                property.Owner = action.action_trace.act.data.a54;
                property.Status = Consts.PROP_STATUS_OWNED;
                property.MintedOn = action.block_time;
                property.MintedBy = action.action_trace.act.data.a54;
                _localDataManager.UpsertProperty(property);
            }
            else
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer.cs - ProcessMintingAction - Property Not Found", string.Format("a45 (PropId): {0}, p44 (Amount): {1}, a54 (Minter EOS): {2}, memo: {3}, Trx_id: {4}", action.action_trace.act.data.a45, action.action_trace.act.data.p44, action.action_trace.act.data.a54, action.action_trace.act.data.memo, action.action_trace.trx_id));
            }
        }

        private void ProcessSendAction(PlayUplandMeAction action)
        {
            if (_registeredUserEOSAccounts.Any(e => e.Item3 == action.action_trace.act.data.p51) && _propertyIdsToWatch.Any(p => p == action.action_trace.act.data.a45))
            {
                try
                {
                    string uplandUsername = _registeredUserEOSAccounts.Where(e => e.Item3 == action.action_trace.act.data.p51).First().Item2;
                    RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);
                    registeredUser.SendUPX += int.Parse(action.action_trace.act.data.p54.Split(".00 UP")[0]);
                    _localDataManager.UpdateRegisteredUser(registeredUser);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer - ProcessActions", string.Format("Failed Adding UPX, a45: {0}, p51: {1}, p54: {2}, ex: {3}", action.action_trace.act.data.a45, action.action_trace.act.data.p51, action.action_trace.act.data.p54, ex.Message));
                }
            }
        }

        private void ProcessSendUPXAction(PlayUplandMeAction action)
        {
            PlayUplandMeData subData = null;
            try
            {
                subData = JsonConvert.DeserializeObject<PlayUplandMeData>(action.action_trace.act.data.data);
            }
            catch
            {
                // Eat it
            }

            if (action.action_trace.act.data.p2 != null && action.action_trace.act.data.p2 == Consts.HornbrodEOSAccount && _registeredUserEOSAccounts.Any(e => e.Item3 == action.action_trace.act.data.p1))
            {
                try
                {
                    string uplandUsername = _registeredUserEOSAccounts.Where(e => e.Item3 == action.action_trace.act.data.p1).First().Item2;
                    RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);
                    registeredUser.SendUPX += int.Parse(action.action_trace.act.data.p45.Split(".00 UP")[0]) - (int)Math.Floor(double.Parse(action.action_trace.act.data.p134.Split("UP")[0]));
                    _localDataManager.UpdateRegisteredUser(registeredUser);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer - Process Send UPX", string.Format("Failed Adding UPX, p1: {0}, p133: {1}, p134: {2}, p2: {3}, p45: {4}, ex: {5}", action.action_trace.act.data.p1, action.action_trace.act.data.p133, action.action_trace.act.data.p134, action.action_trace.act.data.p2, action.action_trace.act.data.p45, ex.Message));
                }
            }
            else if (subData != null && subData.p2 != null && subData.p2 == Consts.HornbrodEOSAccount && _registeredUserEOSAccounts.Any(e => e.Item3 == subData.p1))
            {
                try
                {
                    string uplandUsername = _registeredUserEOSAccounts.Where(e => e.Item3 == subData.p1).First().Item2;
                    RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);
                    registeredUser.SendUPX += int.Parse(subData.p45.Split(".00 UP")[0]) - (int)Math.Floor(double.Parse(subData.p134.Split("UP")[0]));
                    _localDataManager.UpdateRegisteredUser(registeredUser);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("PlayUplandMeSurfer - Process Send UPX Sub", string.Format("Failed Adding UPX, p1: {0}, p133: {1}, p134: {2}, p2: {3}, p45: {4}, ex: {5}", subData.p1, subData.p133, subData.p134, subData.p2, subData.p45, ex.Message));
                }
            }
            else
            {
                _localDataManager.CreateErrorLog("PlayUplandMeSurfer - Failed Process Send UPX", string.Format("Failed Adding UPX, trxId: {0}", action.action_trace.trx_id));
            }
        }

        private async Task<Property> TryToLoadPropertyById(long propertyId)
        {
            // Load the neighborhoods if not done already
            if (_neighborhoods.Count == 0)
            {
                _neighborhoods = _localDataManager.GetNeighborhoods();
            }

            UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(propertyId);

            // PropId does not exist
            if (uplandProperty.Prop_Id == 0 || uplandProperty.Full_Address == null)
            {
                return new Property();
            }

            Property property = UplandMapper.Map(uplandProperty);

            property.NeighborhoodId = _localDataManager.GetNeighborhoodIdForProp(_neighborhoods, property);

            return property;
        }

        private void SetMintsOnRestOfNeighborhood(Property property)
        {
            // pointless on a null mint property.
            if (property.Mint == 0)
            {
                return;
            }

            // Set the other neighborhood mints
            double perUp2Rate = Math.Round(property.Mint / property.Size);

            List<Property> neighborhoodProperties = _localDataManager.GetPropertiesByCityId(property.CityId)
                .Where(p => p.Mint == 0 && p.NeighborhoodId == property.NeighborhoodId && p.Status != Consts.PROP_STATUS_LOCKED).ToList();

            foreach (Property hoodProp in neighborhoodProperties)
            {
                hoodProp.Mint = hoodProp.Size * perUp2Rate;
                _localDataManager.UpsertProperty(hoodProp);
            }
        }
    }
}
