using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upland.Infrastructure.Blockchain;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public class ResyncProcessor
    {
        private readonly LocalDataManager localDataManager;
        private readonly UplandApiManager uplandApiManager;

        public ResyncProcessor()
        {
            localDataManager = new LocalDataManager();
            uplandApiManager = new UplandApiManager();
        }

        public async Task ResyncPropsList(string action, string propList)
        {
            List<long> propIds = new List<long>();
            foreach (string id in propList.Split(","))
            {
                propIds.Add(long.Parse(id));
            }

            List<Property> localProperties = localDataManager.GetProperties(propIds);

            if (action == "SetOwner")
            {
                await Process_SetOwner(localProperties);
            }
            else if (action == "SetMinted")
            {
                Process_SetMinted(localProperties);
            }
            else if (action == "SetMonthlyEarnings")
            {
                await Process_SetMonthlyEarnings(localProperties);
            }
            else if (action == "SetForSale")
            {
                await Process_SetForSale(localProperties);
            }
            else if (action == "FullResync")
            {
                await Process_FullResync(localProperties);
            }
        }

        private async Task Process_SetOwner(List<Property> localProperties) 
        {
            int count = 0;
            foreach (Property localProperty in localProperties)
            {
                UplandProperty uplandProperty = await uplandApiManager.GetUplandPropertyById(localProperty.Id);

                Process_Single_SetOwner(localProperty, uplandProperty);
                count++;
            }
        }

        private void Process_Single_SetOwner(Property localProperty, UplandProperty uplandProperty)
        {
            if (uplandProperty.status == Consts.PROP_STATUS_LOCKED)
            {
                localProperty.Status = uplandProperty.status;
                localProperty.Owner = null;
                localDataManager.DeleteSaleHistoryByPropertyId(localProperty.Id);
                localDataManager.UpsertProperty(localProperty);
            }
            else if (uplandProperty.status == Consts.PROP_STATUS_OWNED || uplandProperty.status == Consts.PROP_STATUS_FORSALE)
            {
                localProperty.Owner = uplandProperty.owner;
                localProperty.Status = Consts.PROP_STATUS_OWNED;
                localDataManager.UpsertProperty(localProperty);
            }
        }

        private void Process_SetMinted(List<Property> localProperties) 
        {
            int count = 0;
            foreach (Property localProperty in localProperties)
            {
                Process_Single_SetMinted(localProperty);
                count++;
            }
        }

        private void Process_Single_SetMinted(Property localProperty)
        {
            localProperty.MintedBy = localProperty.Owner;
            localProperty.MintedOn = new DateTime(2021, 12, 22, 00, 00, 00);

            localDataManager.UpsertProperty(localProperty);
        }

        private async Task Process_SetMonthlyEarnings(List<Property> localProperties) 
        {
            int count = 0;
            foreach (Property localProperty in localProperties)
            {
                UplandProperty uplandProperty = await uplandApiManager.GetUplandPropertyById(localProperty.Id);

                Process_Single_SetMonthlyEarnings(localProperty, uplandProperty);
                count++;
            }
        }

        private void Process_Single_SetMonthlyEarnings(Property localProperty, UplandProperty uplandProperty)
        {
            if (uplandProperty.status == Consts.PROP_STATUS_LOCKED)
            {
                localDataManager.DeleteSaleHistoryByPropertyId(localProperty.Id);
                localProperty.Status = Consts.PROP_STATUS_LOCKED;
            }

            localProperty.MonthlyEarnings = uplandProperty.Yield_Per_Hour.HasValue ? uplandProperty.Yield_Per_Hour.Value * 720 : 0;

            localDataManager.UpsertProperty(localProperty);
        }

        private async Task Process_SetForSale(List<Property> localProperties) 
        {
            int count = 0;
            foreach (Property localProperty in localProperties)
            {
                UplandProperty uplandProperty = await uplandApiManager.GetUplandPropertyById(localProperty.Id);

                Process_Single_SetForSale(localProperty, uplandProperty);

                count++;
            }
        }

        private void Process_Single_SetForSale(Property localProperty, UplandProperty uplandProperty)
        {
            if (uplandProperty.status == Consts.PROP_STATUS_FORSALE)
            {
                localProperty.Status = Consts.PROP_STATUS_FORSALE;
                bool foundSale = Process_Single_SetForSale_CleanSaleHistory(localProperty, uplandProperty);

                if (!foundSale)
                {
                    Process_Single_SetForSale_CreateMissingSale(localProperty, uplandProperty);
                }

                localProperty.Owner = uplandProperty.owner;
            }
            else
            {
                List<SaleHistoryEntry> sales = localDataManager.GetRawSaleHistoryByPropertyId(localProperty.Id).OrderByDescending(s => s.DateTime).ToList();
                localProperty.Status = uplandProperty.status;
                localProperty.Owner = uplandProperty.owner;
                foreach (SaleHistoryEntry entry in sales)
                {
                    if (entry.SellerEOS != null && entry.BuyerEOS == null)
                    {
                        localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }

            localDataManager.UpsertProperty(localProperty);
        }

        private void Process_Single_SetForSale_CreateMissingSale(Property localProperty, UplandProperty uplandProperty)
        {
            SaleHistoryEntry newEntry = new SaleHistoryEntry
            {
                Id = null,
                DateTime = DateTime.UtcNow,
                SellerEOS = uplandProperty.owner,
                BuyerEOS = null,
                PropId = localProperty.Id,
                OfferPropId = null,
                Offer = false,
                Accepted = false
            };

            if (uplandProperty.on_market.currency == "UPX")
            {
                newEntry.Amount = double.Parse(uplandProperty.on_market.token.Split(" UP")[0]);
                newEntry.AmountFiat = null;
            }
            else
            {
                newEntry.Amount = null;
                newEntry.AmountFiat = double.Parse(uplandProperty.on_market.fiat.Split(" FI")[0]);
            }

            localProperty.Owner = uplandProperty.owner;

            localDataManager.UpsertSaleHistory(newEntry);
        }

        private bool Process_Single_SetForSale_CleanSaleHistory(Property localProperty, UplandProperty uplandProperty)
        {
            List<SaleHistoryEntry> sales = localDataManager.GetRawSaleHistoryByPropertyId(localProperty.Id).OrderByDescending(s => s.DateTime).ToList();
            bool foundSale = false;

            foreach (SaleHistoryEntry entry in sales)
            {
                if (entry.SellerEOS != null && entry.BuyerEOS == null)
                {
                    if (entry.SellerEOS != uplandProperty.owner)
                    {
                        localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                        continue;
                    }

                    if (uplandProperty.on_market.currency == "UPX")
                    {
                        if (entry.Amount != double.Parse(uplandProperty.on_market.token.Split(" UP")[0]) || foundSale)
                        {
                            localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                        }
                        else
                        {
                            foundSale = true;
                        }
                    }
                    else
                    {
                        if (entry.AmountFiat != double.Parse(uplandProperty.on_market.fiat.Split(" FI")[0]) || foundSale)
                        {
                            localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                        }
                        else
                        {
                            foundSale = true;
                        }
                    }
                }
            }

            return foundSale;
        }

        private async Task Process_FullResync(List<Property> localProperties)
        {
            int count = 0;
            foreach (Property localProperty in localProperties)
            {
                UplandProperty uplandProperty = await uplandApiManager.GetUplandPropertyById(localProperty.Id);

                Process_Single_FullResync(localProperty, uplandProperty);

                count++;
            }
        }

        private void Process_Single_FullResync(Property localProperty, UplandProperty uplandProperty)
        {
            if (uplandProperty.status == Consts.PROP_STATUS_LOCKED && localProperty.Status != Consts.PROP_STATUS_LOCKED)
            {
                localProperty.Status = uplandProperty.status;
                localProperty.Owner = null;
                localDataManager.DeleteSaleHistoryByPropertyId(localProperty.Id);
                localDataManager.UpsertProperty(localProperty);
            }

            if (uplandProperty.status == Consts.PROP_STATUS_FORSALE && localProperty.Status != Consts.PROP_STATUS_FORSALE)
            {
                if (localProperty.Status == Consts.PROP_STATUS_LOCKED)
                {
                    Process_Single_SetMonthlyEarnings(localProperty, uplandProperty);
                    localProperty = localDataManager.GetProperty(localProperty.Id);
                }

                Process_Single_SetForSale(localProperty, uplandProperty);
            }

            if (uplandProperty.status == Consts.PROP_STATUS_UNLOCKED && localProperty.Status != Consts.PROP_STATUS_UNLOCKED)
            {
                if(localProperty.Status == Consts.PROP_STATUS_LOCKED)
                {
                    Process_Single_SetMonthlyEarnings(localProperty, uplandProperty);
                    localProperty = localDataManager.GetProperty(localProperty.Id);
                }

                Process_Single_SetForSale(localProperty, uplandProperty); 
            }

            if (uplandProperty.status == Consts.PROP_STATUS_OWNED && localProperty.Status != Consts.PROP_STATUS_OWNED)
            {
                if (localProperty.Status == Consts.PROP_STATUS_LOCKED)
                {
                    Process_Single_SetMonthlyEarnings(localProperty, uplandProperty);
                    localProperty = localDataManager.GetProperty(localProperty.Id);
                }

                Process_Single_SetForSale(localProperty, uplandProperty);
            }
        }
    }
}
