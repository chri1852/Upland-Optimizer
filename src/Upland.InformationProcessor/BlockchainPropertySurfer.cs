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

        private const string Status_Owned = "Owned";
        private const string Status_ForSale = "For Sale";
        private const string Status_Unlocked = "Unlocked";

        public BlockchainPropertySurfer()
        {
            localDataManager = new LocalDataManager();
            blockchainManager = new BlockchainManager();
        }

        public async Task BuildBlockChainFromBegining()
        {
            // Upland went live on the blockchain on 2019-06-06 11:51:37
            DateTime startDate = new DateTime(2019, 06, 04, 00, 00, 00);

            await BuildBlockChainFromDate(startDate);
        }

        public async Task BuildBlockChainFromDate(DateTime startDate)
        {

            // Advance a day at a time, unless there are more props
            int minutesToMoveFoward = 14400;
            bool continueLoad = true;
            long maxGlobalSequence = long.Parse(localDataManager.GetConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE));

            while(continueLoad)
            {
                List<HistoryAction> actions = new List<HistoryAction>();

                // Have to do this in a retry loop due to timeouts
                bool retry = true;
                while (retry)
                {
                    Thread.Sleep(5000);
                    actions = await blockchainManager.GetPropertyActionsFromTime(startDate, minutesToMoveFoward);
                    if (actions != null)
                    {
                        retry = false;
                    }
                }

                if (actions.Count == 0)
                {
                    startDate = startDate.AddMinutes(minutesToMoveFoward);
                }
                else
                {
                    ProcessActions(actions, maxGlobalSequence);

                    maxGlobalSequence = actions.Max(a => a.global_sequence);
                    if (actions.Count < 1000)
                    {
                        startDate = startDate.AddMinutes(minutesToMoveFoward);
                    }
                    else
                    {
                        startDate = actions.Max(a => a.timestamp);
                    }
                }

                if (startDate >= DateTime.Now)
                {
                    continueLoad = false;
                }
            }

            // Update the Max Global Sequence seen, this ensures we don't process the same event twice
            //localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE, maxGlobalSequence.ToString());
        }

        private void ProcessActions(List<HistoryAction> actions, long maxGlobalSequence)
        {
            foreach(HistoryAction action in actions)
            {
                if (action.global_sequence < maxGlobalSequence )
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
            }
        }

        private void ProcessDeleteVisitorAction(HistoryAction action)
        {
            localDataManager.DeleteSaleHistoryByBuyerEOSAccount(action.act.data.p52);
            string uplandUsername = localDataManager.GetUplandUsernameByEOSAccount(action.act.data.p52);
            List<Property> properties = localDataManager.GetPropertiesByUplandUsername(uplandUsername);
            
            foreach (Property prop in properties)
            {
                prop.Owner = null;
                prop.Status = Status_Unlocked;
                localDataManager.UpsertProperty(prop);
            }
        }

        private void ProcessBecomeUplanderAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }

        private void ProcessBuyForFiatAction(HistoryAction action)
        {
            long propId = long.Parse(action.act.data.a45);
            List<SaleHistoryEntry> allEntries = localDataManager.GetSaleHistoryByPropertyId(propId);
            SaleHistoryEntry buyEntry = allEntries
                .Where(e => e.BuyerEOS == null && !e.Offer && e.AmountFiat > 0)
                .OrderByDescending(e => e.DateTime)
                .First();

            buyEntry.BuyerEOS = action.act.data.p14;
            localDataManager.UpsertSaleHistory(buyEntry);

            foreach (SaleHistoryEntry entry in allEntries)
            {
                if (entry.Id != buyEntry.Id && (entry.BuyerEOS == null || entry.SellerEOS == null))
                {
                    localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                }
            }

            Property property = localDataManager.GetProperty(propId);
            if (property != null)
            {
                property.Status = Status_Owned;
                property.Owner = localDataManager.GetUplandUsernameByEOSAccount(action.act.data.p14);
                localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessRemoveFromSaleAction(HistoryAction action)
        {
            long propId = long.Parse(action.act.data.a45);
            List<SaleHistoryEntry> historyEntries = localDataManager.GetSaleHistoryByPropertyId(propId);

            foreach(SaleHistoryEntry entry in historyEntries)
            {
                if (entry.SellerEOS == action.act.data.a54 && entry.BuyerEOS == null)
                {
                    localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                }
            }

            Property property = localDataManager.GetProperty(propId);

            if (property != null)
            {
                property.Status = Status_Owned;
                localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessPlaceForSaleAction(HistoryAction action)
        {
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
                historyEntry.AmountFiat = double.Parse(action.act.data.p11.Split("  FI")[0]);
                historyEntry.Amount = null;
            }
            else
            {
                historyEntry.Amount = double.Parse(action.act.data.p11.Split("  UP")[0]);
                historyEntry.AmountFiat = null;
            }

            localDataManager.UpsertSaleHistory(historyEntry);

            Property property = localDataManager.GetProperty(historyEntry.PropId);

            if (property != null)
            {
                property.Status = Status_ForSale;
                localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessPurchaseAction(HistoryAction action)
        {
            long propId = long.Parse(action.act.data.a45);
            List<SaleHistoryEntry> allEntries = localDataManager.GetSaleHistoryByPropertyId(propId);
            SaleHistoryEntry buyEntry = allEntries
                .Where(e => e.BuyerEOS == null && !e.Offer && e.Amount == double.Parse(action.act.data.p24.Split(" UP")[0]))
                .OrderByDescending(e => e.DateTime)
                .First();

            buyEntry.BuyerEOS = action.act.data.p14;
            localDataManager.UpsertSaleHistory(buyEntry);

            foreach(SaleHistoryEntry entry in allEntries)
            {
                if(entry.Id != buyEntry.Id && (entry.BuyerEOS == null || entry.SellerEOS == null))
                {
                    localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                }
            }

            string uplandUsername = action.act.data.memo.Split(" notarizes that Upland user ")[1].Split(" with EOS account ")[0];
            localDataManager.UpsertEOSUser(action.act.data.p14, uplandUsername);

            Property property = localDataManager.GetProperty(propId);

            if (property != null)
            {
                property.Status = Status_Owned;
                property.Owner = localDataManager.GetUplandUsernameByEOSAccount(action.act.data.p14);
                localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessOfferResolutionAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }

        private void ProcessOfferAction(HistoryAction action)
        {
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
            else
            {
                historyEntry.OfferPropId = long.Parse(action.act.data.p21[1]);
                historyEntry.AmountFiat = null;
                historyEntry.Amount = null;
            }

            localDataManager.UpsertSaleHistory(historyEntry);
        }

        private void ProcessMintingAction(HistoryAction action)
        {
            string uplandUsername = action.act.data.memo.Split(" notarizes that Upland user ")[1].Split(" with corresponding EOS account ")[0];
            Property property = localDataManager.GetProperty(long.Parse(action.act.data.a45));

            localDataManager.UpsertEOSUser(action.act.data.a54, uplandUsername);

            if (property != null)
            {
                property.Owner = uplandUsername;
                property.Status = Status_Owned;
                localDataManager.UpsertProperty(property);
            }
        }
    }
}
