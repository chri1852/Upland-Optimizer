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
        private const string Status_ForSale = "For sale";
        private const string Status_Unlocked = "Unlocked";

        public BlockchainPropertySurfer()
        {
            localDataManager = new LocalDataManager();
            blockchainManager = new BlockchainManager();
        }

        public async Task BuildBlockChainFromBegining()
        {
            // Upland went live on the blockchain on 2019-06-06 11:51:37
            DateTime startDate = new DateTime(2019, 06, 05, 01, 00, 00);

            //DateTime startDate = new DateTime(2020, 11, 16, 00, 56, 00);
          //  localDataManager.SetHistoricalCityStats(new DateTime(2020, 03, 11));

            await BuildBlockChainFromDate(startDate);
        }

        public async Task BuildBlockChainFromDate(DateTime startDate)
        {
            DateTime historyTimeStamp = new DateTime(startDate.Year, startDate.Month, startDate.Day);

            // Advance a day at a time, unless there are more props
            int minutesToMoveFoward = 1440;
            bool continueLoad = true;

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

                if (startDate >= DateTime.Now)
                {
                    continueLoad = false;
                }
            }
        }

        private void ProcessActions(List<HistoryAction> actions)
        {
            long maxGlobalSequence = long.Parse(localDataManager.GetConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE));

            foreach (HistoryAction action in actions)
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
            try
            {
                string uplandUsername = localDataManager.GetUplandUsernameByEOSAccount(action.act.data.p52);

                localDataManager.UpdateSaleHistoryVistorToUplander(action.act.data.p52, action.act.data.p53);

                localDataManager.UpsertEOSUser(action.act.data.p53, uplandUsername);
            }
            catch
            {
                // Just Eat it
            }
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
            if (property != null && property.Address != null && property.Address != "")
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

            if (property != null && property.Address != null && property.Address != "")
            {
                property.Owner = localDataManager.GetUplandUsernameByEOSAccount(action.act.data.a54);
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
                historyEntry.AmountFiat = double.Parse(action.act.data.p3.Split(" FI")[0]);
                historyEntry.Amount = null;
            }
            else
            {
                historyEntry.Amount = double.Parse(action.act.data.p11.Split(" UP")[0]);
                historyEntry.AmountFiat = null;
            }

            Property property = localDataManager.GetProperty(historyEntry.PropId);

            if (property != null && property.Address != null && property.Address != "")
            {
                localDataManager.UpsertSaleHistory(historyEntry);
                property.Status = Status_ForSale;
                property.Owner = localDataManager.GetUplandUsernameByEOSAccount(action.act.data.a54);
                localDataManager.UpsertProperty(property);
            }
        }

        private void ProcessPurchaseAction(HistoryAction action)
        {
            long propId = long.Parse(action.act.data.a45);

            Property property = localDataManager.GetProperty(propId);
            if (property == null || property.Address == null || property.Address == "")
            {
                return;
            }

            List<SaleHistoryEntry> allEntries = localDataManager.GetSaleHistoryByPropertyId(propId);
            List<SaleHistoryEntry> buyEntries = allEntries
                .Where(e => e.BuyerEOS == null && !e.Offer && e.Amount == double.Parse(action.act.data.p24.Split(" UP")[0]))
                .OrderByDescending(e => e.DateTime).ToList();

            if (buyEntries.Count == 0)
            {
                return;
            }

            buyEntries.First().BuyerEOS = action.act.data.p14;
            localDataManager.UpsertSaleHistory(buyEntries.First());

            foreach(SaleHistoryEntry entry in allEntries)
            {
                if(entry.Id != buyEntries.First().Id && (entry.BuyerEOS == null || entry.SellerEOS == null))
                {
                    localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                }
            }

            string uplandUsername = action.act.data.memo.Split(" notarizes that Upland user ")[1].Split(" with EOS account ")[0];
            localDataManager.UpsertEOSUser(action.act.data.p14, uplandUsername);

            property.Status = Status_Owned;
            property.Owner = localDataManager.GetUplandUsernameByEOSAccount(action.act.data.p14);
            localDataManager.UpsertProperty(property);
        }

        private void ProcessOfferResolutionAction(HistoryAction action)
        {
            if(Regex.Match(action.act.data.memo, @"\) and Upland user ").Success)
            {
                int propOneCityId = 1;
                int propTwoCityId = 1;
                string propOneAddress = "";
                string propTwoAddress = "";
                string propOneOwner = "";
                string propTwoOwner = "";

                if (Regex.Match(action.act.data.memo, "^[^,]+,[^,]+,[^,]+,[^,]+,[^,]+$").Success)
                {
                    propOneCityId = Consts.Cities.Where(c => c.Value == action.act.data.memo.Split(", ")[1]).First().Key;
                    propOneAddress = action.act.data.memo.Split(" owns ")[1].Split(", ")[0];

                    propTwoCityId = Consts.Cities.Where(c => c.Value == action.act.data.memo.Split(", ")[3]).First().Key;
                    propTwoAddress = action.act.data.memo.Split(" owns ")[2].Split(", ")[0];
                }
                else
                {
                    propOneAddress = action.act.data.memo.Split(" owns ")[1].Split(" (ini")[0];
                    propTwoAddress = action.act.data.memo.Split(" owns ")[2].Split(" (ini")[0];
                }

                propOneOwner = action.act.data.memo.Split("that Upland user ")[1].Split(" (EOS account ")[0];
                propTwoOwner = action.act.data.memo.Split("and Upland user ")[1].Split(" (EOS account ")[0];

                Property propOne = localDataManager.GetPropertyByCityIdAndAddress(propOneCityId, propOneAddress);
                Property propTwo = localDataManager.GetPropertyByCityIdAndAddress(propTwoCityId, propTwoAddress);

                propOne.Owner = propOneOwner;
                propOne.Status = Status_Owned;
                if (propOne != null && propOne.Address != null && propOne.Address != "")
                {
                    localDataManager.UpsertProperty(propOne);
                }

                propTwo.Owner = propTwoOwner;
                propTwo.Status = Status_Owned;
                if (propTwo != null && propTwo.Address != null && propTwo.Address != "")
                {
                    localDataManager.UpsertProperty(propTwo);
                }

                localDataManager.UpsertEOSUser(action.act.data.memo.Split("(EOS account ")[1].Split(") owns")[0], propOne.Owner);
                localDataManager.UpsertEOSUser(action.act.data.memo.Split("(EOS account ")[2].Split(") now owns ")[0], propTwo.Owner);

                List<SaleHistoryEntry> allEntriesOne = localDataManager.GetSaleHistoryByPropertyId(propOne.Id);
                List<SaleHistoryEntry> allEntriesTwo = localDataManager.GetSaleHistoryByPropertyId(propTwo.Id);

                SaleHistoryEntry buyEntry = allEntriesTwo
                    .Where(e => e.SellerEOS == null && e.Offer && e.BuyerEOS == action.act.data.memo.Split("(EOS account ")[2].Split(") now owns ")[0])
                    .OrderByDescending(e => e.DateTime)
                    .FirstOrDefault();

                if (buyEntry != null)
                {
                    buyEntry.SellerEOS = action.act.data.memo.Split("(EOS account ")[1].Split(") owns")[0];
                    buyEntry.Accepted = true;
                    localDataManager.UpsertSaleHistory(buyEntry);
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
                Property prop = localDataManager.GetPropertyByCityIdAndAddress(Consts.Cities.Where(c => c.Value == action.act.data.memo.Split(", ")[1]).First().Key, action.act.data.memo.Split(" owns ")[1].Split(", ")[0]);
                if(prop == null || prop.Id == 0 || prop.Address == null || prop.Address == "")
                {
                    return;
                }
                prop.Owner = action.act.data.memo.Split("that Upland user ")[1].Split(" with EOS acc")[0];
                prop.Status = Status_Owned;

                localDataManager.UpsertProperty(prop);

                localDataManager.UpsertEOSUser(action.act.data.memo.Split("EOS account ")[1].Split(" owns ")[0], prop.Owner);

                List<SaleHistoryEntry> allEntries = localDataManager.GetSaleHistoryByPropertyId(prop.Id);
                SaleHistoryEntry buyEntry = allEntries
                    .Where(e => e.SellerEOS == null && e.Offer && e.BuyerEOS == action.act.data.memo.Split("EOS account ")[1].Split(" owns ")[0])
                    .OrderByDescending(e => e.DateTime)
                    .FirstOrDefault();

                if (buyEntry != null)
                {
                    buyEntry.SellerEOS = action.act.data.p25;
                    buyEntry.Accepted = true;
                    localDataManager.UpsertSaleHistory(buyEntry);
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

            Property property = localDataManager.GetProperty(long.Parse(action.act.data.p15));

            if (property != null)
            {
                localDataManager.UpsertSaleHistory(historyEntry);
            }
        }

        private void ProcessMintingAction(HistoryAction action)
        {
            string uplandUsername = action.act.data.memo.Split(" notarizes that Upland user ")[1].Split(" with corresponding EOS account ")[0];
            Property property = localDataManager.GetProperty(long.Parse(action.act.data.a45));

            localDataManager.UpsertEOSUser(action.act.data.a54, uplandUsername);

            if (property != null && property.Address != null && property.Address != "")
            {
                property.Owner = uplandUsername;
                property.Status = Status_Owned;
                localDataManager.UpsertProperty(property);
            }
        }
    }
}
