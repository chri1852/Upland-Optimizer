using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Upland.Infrastructure.Blockchain;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.BlockchainTypes;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class BlockchainPropertySurfer
    {
        private readonly LocalDataManager localDataManager;
        private readonly BlockchainManager blockchainManager;
        private bool isProcessing;

        public BlockchainPropertySurfer()
        {
            localDataManager = new LocalDataManager();
            blockchainManager = new BlockchainManager();
            isProcessing = false;
        }

        public async Task RunBlockChainUpdate()
        {
            DateTime lastSaleUpdate = localDataManager.GetLastSaleHistoryDateTime();

            try
            {
                await BuildBlockChainFromDate(lastSaleUpdate);
            }
            catch (Exception ex)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - RunBlockChainUpdate", ex.Message);
                this.isProcessing = false;
            }
        }

        public async Task BuildBlockChainFromBegining()
        {
            // Upland went live on the blockchain on 2019-06-06 11:51:37
            DateTime startDate = new DateTime(2019, 06, 05, 01, 00, 00);

            await BuildBlockChainFromDate(startDate);
        }

        public async Task BuildBlockChainFromDate(DateTime startDate)
        {
            bool enableUpdates = bool.Parse(localDataManager.GetConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES));

            if (!enableUpdates || this.isProcessing)
            {
                return;
            }

            this.isProcessing = true;

            DateTime historyTimeStamp = localDataManager.GetLastHistoricalCityStatusDate();

            if(historyTimeStamp == DateTime.MinValue)
            {
                historyTimeStamp = new DateTime(startDate.Year, startDate.Month, startDate.Day);
            }
            else
            {
                historyTimeStamp = new DateTime(historyTimeStamp.Year, historyTimeStamp.Month, historyTimeStamp.Day);
            }

            // Advance a day at a time, unless there are more props
            //int minutesToMoveFoward = 1440;
            int minutesToMoveFoward = 360; // One Hours
            bool continueLoad = true;

            while(continueLoad)
            {
                List<HistoryAction> actions = new List<HistoryAction>();

                // Have to do this in a retry loop due to timeouts
                bool retry = true;
                while (retry)
                {
                    try
                    {
                        actions = await blockchainManager.GetPropertyActionsFromTime(startDate, minutesToMoveFoward);
                        if (actions != null)
                        {
                            retry = false;
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - BuildBlockChainFromDate - Loop", ex.Message);
                        Thread.Sleep(5000);
                    }
                }

                if (actions.Count == 0)
                {
                    startDate = startDate.AddMinutes(minutesToMoveFoward);
                }
                else
                {
                    actions = actions.OrderBy(a => a.global_sequence).ToList();
                    ProcessActions(actions);

                    if (actions.Count < 1000)
                    {
                        startDate = startDate.AddMinutes(minutesToMoveFoward);
                    }
                    else
                    {
                        startDate = actions.Max(a => a.timestamp);
                    }

                    if(historyTimeStamp.AddDays(1).Ticks < startDate.Ticks)
                    {
                        historyTimeStamp = new DateTime(startDate.Year, startDate.Month, startDate.Day);
                        localDataManager.SetHistoricalCityStats(historyTimeStamp);
                    }
                }

                if (startDate >= DateTime.UtcNow)
                {
                    continueLoad = false;
                }
            }

            this.isProcessing = false;
        }

        private void ProcessActions(List<HistoryAction> actions)
        {
            long maxGlobalSequence = long.Parse(localDataManager.GetConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE));

            foreach (HistoryAction action in actions)
            {
                if (action.global_sequence <= maxGlobalSequence )
                {
                    // We've already processed this event
                    continue;
                }

                switch(action.act.name)
                {
                    case "a4":
                        ProcessMintingAction(action);
                        break;
                    case "n12":
                        ProcessOfferAction(action);
                        break;
                    case "n13":
                        ProcessOfferResolutionAction(action);
                        break;
                    case "n5":
                        ProcessPurchaseAction(action);
                        break;
                    case "n2":
                        ProcessPlaceForSaleAction(action);
                        break;
                    case "n4":
                        ProcessRemoveFromSaleAction(action);
                        break;
                    case "n52":
                        ProcessBuyForFiatAction(action);
                        break;
                    case "n33":
                        ProcessBecomeUplanderAction(action);
                        break;
                    case "n34":
                        ProcessDeleteVisitorAction(action);
                        break;
                    default:
                        continue;
                }
                
                if (action.global_sequence > maxGlobalSequence)
                {
                    maxGlobalSequence = action.global_sequence;
                    // Update the Max Global Sequence seen, this ensures we don't process the same event twice
                    localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE, maxGlobalSequence.ToString());
                }
            }
        }

        private void ProcessDeleteVisitorAction(HistoryAction action)
        {
            if (action.act.data.p52 == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessDeleteVisitorAction", string.Format("p52 (Visitor EOS): {0}, Trx_id: {1}", action.act.data.p52, action.trx_id));
                return;
            }

            List<Property> properties = localDataManager.GetPropertiesByUplandUsername(action.act.data.p52);
            localDataManager.DeleteEOSUser(action.act.data.p52);

            foreach (Property prop in properties)
            {
                prop.Owner = null;
                prop.Status = Consts.PROP_STATUS_UNLOCKED;
                prop.MintedBy = null;
                prop.MintedOn = null;
                localDataManager.UpsertProperty(prop);
            }
        }

        private void ProcessBecomeUplanderAction(HistoryAction action)
        {
            if (action.act.data.p53 == null || action.act.data.p52 == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessBecomeUplanderAction", string.Format("p53 (New EOS): {0}, p52 (Visitor EOS): {1}, Trx_id: {2}", action.act.data.p53, action.act.data.p52, action.trx_id));
                return;
            }
            string uplandUsername = action.act.data.memo.Split(" notarizes that Upland user ")[1].Split(" with corresponding ")[0];

            List<Property> properties = localDataManager.GetPropertiesByUplandUsername(action.act.data.p52);

            foreach (Property prop in properties)
            {
                prop.Owner = action.act.data.p53;
                localDataManager.UpsertProperty(prop);
            }

            localDataManager.DeleteEOSUser(action.act.data.p52);

            localDataManager.UpsertEOSUser(action.act.data.p53, uplandUsername, action.timestamp);
        }

        private void ProcessBuyForFiatAction(HistoryAction action)
        {
            if (action.act.data.a45 == null || action.act.data.p14 == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessBuyForFiatAction - Missing Data", string.Format("a45 (propId): {0}, p14 (Buyer EOS): {1}, Trx_id: {2}", action.act.data.p53, action.act.data.p52, action.trx_id));
                return;
            }

            long propId = long.Parse(action.act.data.a45);
            List<SaleHistoryEntry> allEntries = localDataManager.GetRawSaleHistoryByPropertyId(propId);
            List<SaleHistoryEntry> buyEntries = allEntries
                .Where(e => e.BuyerEOS == null && !e.Offer && e.AmountFiat > 0)
                .OrderByDescending(e => e.DateTime).ToList();

            if (buyEntries.Count == 0)
            {
                // If for some reason the sale already got set, check to see if it is there
                buyEntries = allEntries
                .Where(e => e.BuyerEOS == action.act.data.p14 && !e.Offer && e.AmountFiat > 0)
                .OrderByDescending(e => e.DateTime).ToList();

                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessBuyForFiatAction - Missing Buy", string.Format("a45 (propId: {0}, p14 (Buyer EOS): {1}, Trx_id: {2}", action.act.data.p53, action.act.data.p52, action.trx_id));
                if (buyEntries.Count > 0)
                {
                    localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessBuyForFiatAction - Found Filled Missing Buy", string.Format("BuyEntryId: {0}, BuyEntriesCount: {1}", buyEntries[0].Id, buyEntries.Count));
                }
            }

            if (buyEntries.Count > 0)
            {
                buyEntries.First().BuyerEOS = action.act.data.p14;
                buyEntries.First().DateTime = action.timestamp;
                localDataManager.UpsertSaleHistory(buyEntries.First());

                foreach (SaleHistoryEntry entry in allEntries)
                {
                    if (entry.Id != buyEntries.First().Id && (entry.BuyerEOS == null || entry.SellerEOS == null))
                    {
                        localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }

            Property property = localDataManager.GetProperty(propId);
            if (property != null && property.Address != null && property.Address != "")
            {
                property.Status = Consts.PROP_STATUS_OWNED;
                property.Owner = action.act.data.p14;
                localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessRemoveFromSaleAction(HistoryAction action)
        {
            if (action.act.data.a45 == null || action.act.data.a54 == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessRemoveFromSaleAction", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, Trx_id: {2}", action.act.data.a45, action.act.data.a54, action.trx_id));
                return;
            }

            long propId = long.Parse(action.act.data.a45);
            List<SaleHistoryEntry> historyEntries = localDataManager.GetRawSaleHistoryByPropertyId(propId);

            foreach(SaleHistoryEntry entry in historyEntries)
            {
                if (entry.SellerEOS == action.act.data.a54 && entry.BuyerEOS == null)
                {
                    localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                }
            }

            Property property = localDataManager.GetProperty(propId);

            if (property != null && property.Address != null && property.Address != "")
            {
                property.Status = Consts.PROP_STATUS_OWNED;
                localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessPlaceForSaleAction(HistoryAction action)
        {
            if (action.act.data.a45 == null || action.act.data.a54 == null || action.act.data.p11 == null || action.act.data.p3 == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessPlaceForSaleAction", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.act.data.a45, action.act.data.a54, action.act.data.p11, action.act.data.p3, action.trx_id));
                return;
            }

            SaleHistoryEntry historyEntry = new SaleHistoryEntry
            {
                DateTime = action.timestamp,
                SellerEOS = action.act.data.a54,
                BuyerEOS = null,
                PropId = long.Parse(action.act.data.a45),
                Offer = false
            };

            if(Regex.Match(action.act.data.p11, "^0.00 UP").Success)
            {
                historyEntry.AmountFiat = double.Parse(action.act.data.p3.Split(" FI")[0]);
                historyEntry.Amount = null;
            }
            else if (Regex.Match(action.act.data.p3, "^0.00 FI").Success)
            {
                historyEntry.Amount = double.Parse(action.act.data.p11.Split(" UP")[0]);
                historyEntry.AmountFiat = null;
            }
            else
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessPlaceForSaleAction - Failed Parsing Amount", string.Format("a45 (propId): {0}, a54 (Seller EOS): {1}, p11 (UPX): {2}, p3 (USD): {3}, Trx_id: {4}", action.act.data.a45, action.act.data.a54, action.act.data.p11, action.act.data.p3, action.trx_id));
            }

            Property property = localDataManager.GetProperty(historyEntry.PropId);

            if (property != null && property.Address != null && property.Address != "")
            {
                localDataManager.UpsertSaleHistory(historyEntry);
                property.Status = Consts.PROP_STATUS_FORSALE;
                localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessPurchaseAction(HistoryAction action)
        {
            if (action.act.data.p14 == null || action.act.data.a45 == null || action.act.data.p24 == null || action.act.data.memo == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessPurchaseAction", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.act.data.p14, action.act.data.a45, action.act.data.p24, action.act.data.memo, action.trx_id));
                return;
            }

            long propId = long.Parse(action.act.data.a45);

            Property property = localDataManager.GetProperty(propId);
            if (property == null || property.Address == null || property.Address == "")
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessPurchaseAction - Missing Prop", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.act.data.p14, action.act.data.a45, action.act.data.p24, action.act.data.memo, action.trx_id));
                return;
            }

            List<SaleHistoryEntry> allEntries = localDataManager.GetRawSaleHistoryByPropertyId(propId);
            List<SaleHistoryEntry> buyEntries = allEntries
                .Where(e => e.BuyerEOS == null && !e.Offer && e.Amount == double.Parse(action.act.data.p24.Split(" UP")[0]))
                .OrderByDescending(e => e.DateTime).ToList();

            if (buyEntries.Count == 0)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessPurchaseAction - Missing Sale Entry", string.Format("p14 (Buyer EOS): {0}, a45 (PropId): {1}, p24 (Amount): {2}, memo: {3}, Trx_id: {4}", action.act.data.p14, action.act.data.a45, action.act.data.p24, action.act.data.memo, action.trx_id));
            }
            else
            {
                buyEntries.First().BuyerEOS = action.act.data.p14;
                buyEntries.First().DateTime = action.timestamp;
                localDataManager.UpsertSaleHistory(buyEntries.First());
            }

            foreach(SaleHistoryEntry entry in allEntries)
            {
                if (entry.BuyerEOS == null || entry.SellerEOS == null)
                {
                    localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                }
            }

            property.Status = Consts.PROP_STATUS_OWNED;
            property.Owner = action.act.data.p14;
            localDataManager.UpsertProperty(property);
        }

        private void ProcessOfferResolutionAction(HistoryAction action)
        {
            if (action.act.data.p25 == null || action.act.data.memo == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessOfferResolutionAction", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.act.data.p25, action.act.data.memo, action.trx_id));
                return;
            }

            if (Regex.Match(action.act.data.memo, @"\) and Upland user ").Success)
            {
                int propOneCityId = 1;
                int propTwoCityId = 1;
                string propOneAddress = "";
                string propTwoAddress = "";

                if (Regex.Match(action.act.data.memo, "^[^,]+,[^,]+,[^,]+,[^,]+,[^,]+$").Success)
                {
                    propOneCityId = HelperFunctions.GetCityIdByName(action.act.data.memo.Split(", ")[1]);
                    propOneAddress = action.act.data.memo.Split(" owns ")[1].Split(", ")[0];

                    propTwoCityId = HelperFunctions.GetCityIdByName(action.act.data.memo.Split(", ")[3]);
                    propTwoAddress = action.act.data.memo.Split(" owns ")[2].Split(", ")[0];
                }
                else
                {
                    propOneAddress = action.act.data.memo.Split(" owns ")[1].Split(" (ini")[0];
                    propTwoAddress = action.act.data.memo.Split(" owns ")[2].Split(" (ini")[0];
                }

                Property propOne = localDataManager.GetPropertyByCityIdAndAddress(propOneCityId, propOneAddress);
                Property propTwo = localDataManager.GetPropertyByCityIdAndAddress(propTwoCityId, propTwoAddress);

                propOne.Owner = action.act.data.memo.Split("(EOS account ")[1].Split(") owns")[0];
                propOne.Status = Consts.PROP_STATUS_OWNED;
                if (propOne != null && propOne.Address != null && propOne.Address != "")
                {
                    localDataManager.UpsertProperty(propOne);
                }
                else
                {
                    localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessOfferResolutionAction - Could Not Find Prop (SWAP)", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.act.data.p25, action.act.data.memo, action.trx_id));
                }

                propTwo.Owner = action.act.data.memo.Split("(EOS account ")[2].Split(") now owns ")[0];
                propTwo.Status = Consts.PROP_STATUS_OWNED;
                if (propTwo != null && propTwo.Address != null && propTwo.Address != "")
                {
                    localDataManager.UpsertProperty(propTwo);
                }
                else
                {
                    localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessOfferResolutionAction - Could Not Find Prop (SWAP)", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.act.data.p25, action.act.data.memo, action.trx_id));
                }

                List<SaleHistoryEntry> allEntriesOne = localDataManager.GetRawSaleHistoryByPropertyId(propOne.Id);
                List<SaleHistoryEntry> allEntriesTwo = localDataManager.GetRawSaleHistoryByPropertyId(propTwo.Id);

                SaleHistoryEntry buyEntry = allEntriesTwo
                    .Where(e => e.SellerEOS == null && e.Offer && e.BuyerEOS == action.act.data.memo.Split("(EOS account ")[2].Split(") now owns ")[0])
                    .OrderByDescending(e => e.DateTime)
                    .FirstOrDefault();

                if (buyEntry != null)
                {
                    buyEntry.SellerEOS = action.act.data.memo.Split("(EOS account ")[1].Split(") owns")[0];
                    buyEntry.Accepted = true;
                    buyEntry.DateTime = action.timestamp;
                    localDataManager.UpsertSaleHistory(buyEntry);
                }
                else
                {
                    localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessOfferResolutionAction - Could Not Find Buy Entry (SWAP)", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.act.data.p25, action.act.data.memo, action.trx_id));
                }

                foreach (SaleHistoryEntry entry in allEntriesOne)
                {
                    if (entry.BuyerEOS == null || entry.SellerEOS == null)
                    {
                        localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }

                foreach (SaleHistoryEntry entry in allEntriesTwo)
                {
                    if (entry.BuyerEOS == null || entry.SellerEOS == null)
                    {
                        localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }
            else
            {
                string cityName = HelperFunctions.SusOutCityNameByMemoString(action.act.data.memo);
                Property prop = localDataManager.GetPropertyByCityIdAndAddress(
                    HelperFunctions.GetCityIdByName(cityName), 
                    action.act.data.memo.Split(" owns ")[1].Split(string.Format(", {0}", cityName))[0]);

                if(prop == null || prop.Id == 0 || prop.Address == null || prop.Address == "")
                {
                    localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessOfferResolutionAction - Could Not Find Prop", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.act.data.p25, action.act.data.memo, action.trx_id));
                    return;
                }

                prop.Owner = action.act.data.memo.Split("EOS account ")[1].Split(" owns ")[0];
                prop.Status = Consts.PROP_STATUS_OWNED;

                localDataManager.UpsertProperty(prop);

                List<SaleHistoryEntry> allEntries = localDataManager.GetRawSaleHistoryByPropertyId(prop.Id);
                SaleHistoryEntry buyEntry = allEntries
                    .Where(e => e.SellerEOS == null && e.Offer && e.BuyerEOS == action.act.data.memo.Split("EOS account ")[1].Split(" owns ")[0])
                    .OrderByDescending(e => e.DateTime)
                    .FirstOrDefault();

                if (buyEntry != null)
                {
                    buyEntry.SellerEOS = action.act.data.p25;
                    buyEntry.Accepted = true;
                    buyEntry.DateTime = action.timestamp;
                    localDataManager.UpsertSaleHistory(buyEntry);
                }
                else
                {
                    localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessOfferResolutionAction - Could Not Find Buy Entry", string.Format("p25 (Seller EOS): {0}, memo: {1}, Trx_id: {2}", action.act.data.p25, action.act.data.memo, action.trx_id));
                }

                foreach (SaleHistoryEntry entry in allEntries)
                {
                    if (entry.BuyerEOS == null || entry.SellerEOS == null)
                    {
                        localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }
        }

        private void ProcessOfferAction(HistoryAction action)
        {
            if (action.act.data.p23 == null || action.act.data.p15 == null || action.act.data.p21 == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessOfferAction", string.Format("p23 (Buyer EOS): {0}, p15 (PropId): {1}, p21 (Offer): {2}, Trx_id: {3}", action.act.data.p23, action.act.data.p15, string.Join(" ", action.act.data.p21), action.trx_id));
                return;
            }

            SaleHistoryEntry historyEntry = new SaleHistoryEntry
            {
                DateTime = action.timestamp,
                BuyerEOS = action.act.data.p23,
                SellerEOS = null,
                PropId = long.Parse(action.act.data.p15),
                Offer = true
            };

            if (action.act.data.p21[0] == "asset")
            {
                historyEntry.Amount = double.Parse(action.act.data.p21[1].Split(" UP")[0]);
                historyEntry.AmountFiat = null;
            }
            else if(action.act.data.p21[0] == "uint64")
            {
                historyEntry.OfferPropId = long.Parse(action.act.data.p21[1]);
                historyEntry.AmountFiat = null;
                historyEntry.Amount = null;
            }
            else
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessOfferAction - Could Not Parse p21", string.Format("p23 (Buyer EOS): {0}, p15 (PropId): {1}, p21 (Offer): {2}, Trx_id: {3}", action.act.data.p23, action.act.data.p15, string.Join(" ", action.act.data.p21), action.trx_id));
            }

            Property property = localDataManager.GetProperty(long.Parse(action.act.data.p15));

            if (property != null)
            {
                localDataManager.UpsertSaleHistory(historyEntry);
            }
        }

        private void ProcessMintingAction(HistoryAction action)
        {
            if (action.act.data.a45 == null || action.act.data.p44 == null || action.act.data.a54 == null || action.act.data.memo == null)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessMintingAction", string.Format("a45 (PropId): {0}, p44 (Amount): {1}, a54 (Minter EOS): {2}, memo: {3}, Trx_id: {4}", action.act.data.a45, action.act.data.p44, action.act.data.a54, action.act.data.memo, action.trx_id));
                return;
            }

            string uplandUsername = action.act.data.memo.Split(" notarizes that Upland user ")[1].Split(" with corresponding EOS account ")[0];
            Property property = localDataManager.GetProperty(long.Parse(action.act.data.a45));
            Tuple<string, string> existingAccount = localDataManager.GetUplandUsernameByEOSAccount(action.act.data.a54);

            if (existingAccount != null && existingAccount.Item2 != uplandUsername)
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessMintingAction - Found Non Deleted Visitor", string.Format("a45 (PropId): {0}, p44 (Amount): {1}, a54 (Minter EOS): {2}, memo: {3}, ExistingAccountEOS: {4}, ExistingAccountUsername: {5}, Trx_id: {6}", action.act.data.a45, action.act.data.p44, action.act.data.a54, action.act.data.memo, existingAccount.Item1, existingAccount.Item2, action.trx_id));

                // Tried Upserting a EOS Account that already exists, the older one must have been deleted, but not processed yet
                HistoryAction deleteAction = new HistoryAction
                {
                    act = new ActionEntry
                    {
                        data = new ActionData
                        {
                            p52 = existingAccount.Item1
                        }
                    }
                };
                ProcessDeleteVisitorAction(deleteAction);

                localDataManager.UpsertEOSUser(action.act.data.a54, uplandUsername, action.timestamp);
            }
            else if(existingAccount == null)
            {
                localDataManager.UpsertEOSUser(action.act.data.a54, uplandUsername, action.timestamp);
            }

            if (property != null && property.Address != null && property.Address != "")
            {
                property.Owner = action.act.data.a54;
                property.Status = Consts.PROP_STATUS_OWNED;
                property.MintedOn = action.timestamp;
                property.MintedBy = action.act.data.a54;
                localDataManager.UpsertProperty(property);
            }
            else
            {
                localDataManager.CreateErrorLog("BlockchainPropertSurfer.cs - ProcessMintingAction - Property Not Found", string.Format("a45 (PropId): {0}, p44 (Amount): {1}, a54 (Minter EOS): {2}, memo: {3}, Trx_id: {4}", action.act.data.a45, action.act.data.p44, action.act.data.a54, action.act.data.memo, action.trx_id));
            }
        }
    }
}
