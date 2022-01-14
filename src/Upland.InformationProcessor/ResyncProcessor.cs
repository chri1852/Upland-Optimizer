using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public class ResyncProcessor : IResyncProcessor
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IUplandApiManager _uplandApiManager;

        public ResyncProcessor(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;
        }

        public async Task ResyncPropsList(string action, string propList)
        {
            List<long> propIds = new List<long>();
            foreach (string id in propList.Split(","))
            {
                propIds.Add(long.Parse(id));
            }

            List<Property> localProperties = _localDataManager.GetProperties(propIds);

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
            else if (action == "CitySaleResync")
            {
                await Process_CitySaleResync(propList);
            }
            else if (action == "NeighborhoodSaleResync")
            {
                await Process_NeighborhoodSaleResync(propList);
            }
            else if (action == "CollectionSaleResync")
            {
                await Process_CollectionSaleResync(propList);
            }
            else if (action == "BuildingSaleResync")
            {
                await Process_BuildingSaleResync();
            }
            else if (action == "CityUnmintedResync")
            {
                await Process_CityUnmintedResync(propList);
            }
            else if (action == "NeighborhoodUnmintedResync")
            {
                await Process_NeighborhoodUnmintedResync(propList);
            }
            else if (action == "EnclaveFix")
            {
                Process_EnclaveFix();
            }
        }

        private async Task Process_SetOwner(List<Property> localProperties)
        {
            int count = 0;
            foreach (Property localProperty in localProperties)
            {
                UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(localProperty.Id);

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
                _localDataManager.DeleteSaleHistoryByPropertyId(localProperty.Id);
                _localDataManager.UpsertProperty(localProperty);
            }
            else if (uplandProperty.status == Consts.PROP_STATUS_OWNED || uplandProperty.status == Consts.PROP_STATUS_FORSALE)
            {
                localProperty.Owner = uplandProperty.owner;
                localProperty.Status = Consts.PROP_STATUS_OWNED;
                _localDataManager.UpsertProperty(localProperty);
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

            _localDataManager.UpsertProperty(localProperty);
        }

        private async Task Process_SetMonthlyEarnings(List<Property> localProperties)
        {
            int count = 0;
            foreach (Property localProperty in localProperties)
            {
                UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(localProperty.Id);

                Process_Single_SetMonthlyEarnings(localProperty, uplandProperty);
                count++;
            }
        }

        private void Process_Single_SetMonthlyEarnings(Property localProperty, UplandProperty uplandProperty)
        {
            if (uplandProperty.status == Consts.PROP_STATUS_LOCKED)
            {
                _localDataManager.DeleteSaleHistoryByPropertyId(localProperty.Id);
                localProperty.Status = Consts.PROP_STATUS_LOCKED;
            }

            localProperty.Mint = uplandProperty.Yield_Per_Hour.HasValue ? uplandProperty.Yield_Per_Hour.Value * 720 : 0;

            _localDataManager.UpsertProperty(localProperty);
        }

        private async Task Process_SetForSale(List<Property> localProperties)
        {
            int count = 0;
            foreach (Property localProperty in localProperties)
            {
                UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(localProperty.Id);

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
                List<SaleHistoryEntry> sales = _localDataManager.GetRawSaleHistoryByPropertyId(localProperty.Id).OrderByDescending(s => s.DateTime).ToList();
                localProperty.Status = uplandProperty.status;
                localProperty.Owner = uplandProperty.owner;
                foreach (SaleHistoryEntry entry in sales)
                {
                    if (entry.SellerEOS != null && entry.BuyerEOS == null)
                    {
                        _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                    }
                }
            }

            _localDataManager.UpsertProperty(localProperty);
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

            _localDataManager.UpsertSaleHistory(newEntry);
        }

        private bool Process_Single_SetForSale_CleanSaleHistory(Property localProperty, UplandProperty uplandProperty)
        {
            List<SaleHistoryEntry> sales = _localDataManager.GetRawSaleHistoryByPropertyId(localProperty.Id).OrderByDescending(s => s.DateTime).ToList();
            bool foundSale = false;

            foreach (SaleHistoryEntry entry in sales)
            {
                if (entry.SellerEOS != null && entry.BuyerEOS == null)
                {
                    if (entry.SellerEOS != uplandProperty.owner)
                    {
                        _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
                        continue;
                    }

                    if (uplandProperty.on_market.currency == "UPX")
                    {
                        if (entry.Amount != double.Parse(uplandProperty.on_market.token.Split(" UP")[0]) || foundSale)
                        {
                            _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
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
                            _localDataManager.DeleteSaleHistoryById(entry.Id.Value);
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
                UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(localProperty.Id);

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
                _localDataManager.DeleteSaleHistoryByPropertyId(localProperty.Id);
                _localDataManager.UpsertProperty(localProperty);
            }

            if (uplandProperty.status == Consts.PROP_STATUS_FORSALE && localProperty.Status != Consts.PROP_STATUS_FORSALE)
            {
                if (localProperty.Status == Consts.PROP_STATUS_LOCKED)
                {
                    Process_Single_SetMonthlyEarnings(localProperty, uplandProperty);
                    localProperty = _localDataManager.GetProperty(localProperty.Id);
                }

                Process_Single_SetForSale(localProperty, uplandProperty);
            }

            if (uplandProperty.status == Consts.PROP_STATUS_UNLOCKED && localProperty.Status != Consts.PROP_STATUS_UNLOCKED)
            {
                if (localProperty.Status == Consts.PROP_STATUS_LOCKED)
                {
                    Process_Single_SetMonthlyEarnings(localProperty, uplandProperty);
                    localProperty = _localDataManager.GetProperty(localProperty.Id);
                }

                Process_Single_SetForSale(localProperty, uplandProperty);
            }

            if (uplandProperty.status == Consts.PROP_STATUS_OWNED && localProperty.Status != Consts.PROP_STATUS_OWNED)
            {
                if (localProperty.Status == Consts.PROP_STATUS_LOCKED)
                {
                    Process_Single_SetMonthlyEarnings(localProperty, uplandProperty);
                    localProperty = _localDataManager.GetProperty(localProperty.Id);
                }

                Process_Single_SetForSale(localProperty, uplandProperty);
            }
        }

        private async Task Process_CitySaleResync(string cityIds)
        {
            List<int> ids = new List<int>();
            if (cityIds != "0")
            {
                foreach (string id in cityIds.Split(","))
                {
                    ids.Add(int.Parse(id));
                }
            }
            else
            {
                ids = Consts.NON_BULLSHIT_CITY_IDS;
            }

            foreach (int cityId in ids)
            {
                int okayProps = 0;

                Dictionary<long, Property> properties = _localDataManager
                    .GetPropertiesByCityId(cityId)
                    .ToDictionary(p => p.Id, p => p);

                List<UplandForSaleProp> forSaleProps = _localDataManager
                    .GetPropertiesForSale_City(cityId, false)
                    .Where(p => properties.ContainsKey(p.Prop_Id))
                    .OrderBy(p => p.SortValue).ToList();

                foreach (UplandForSaleProp prop in forSaleProps)
                {
                    UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(prop.Prop_Id);
                    Property localProperty = properties[prop.Prop_Id];

                    if (uplandProperty.status != Consts.PROP_STATUS_FORSALE)
                    {
                        Process_Single_SetForSale(localProperty, uplandProperty);
                    }
                    else
                    {
                        if (Process_Single_SetForSale_CleanSaleHistory(localProperty, uplandProperty))
                        {
                            okayProps++;
                        }
                        else
                        {
                            Process_Single_SetForSale(localProperty, uplandProperty);
                        }
                    }

                    if (okayProps == 100)
                    {
                        break;
                    }
                }
            }
        }

        private async Task Process_NeighborhoodSaleResync(string neighborhoodIds)
        {
            List<Neighborhood> neighborhoods = new List<Neighborhood>();
            if (neighborhoodIds != "0")
            {
                neighborhoods = _localDataManager.GetNeighborhoods()
                    .Where(n => neighborhoodIds.Split(",").Select(s => int.Parse(s)).ToList().Contains(n.Id))
                    .ToList();
            }
            else
            {
                neighborhoods = _localDataManager.GetNeighborhoods();
            }

            foreach (Neighborhood neighborhood in neighborhoods)
            {
                int okayProps = 0;

                Dictionary<long, Property> properties = _localDataManager
                    .GetPropertiesByCityId(neighborhood.CityId)
                        .Where(p => p.NeighborhoodId == neighborhood.Id)
                        .ToDictionary(p => p.Id, p => p);

                List<UplandForSaleProp> forSaleProps = _localDataManager
                    .GetPropertiesForSale_Neighborhood(neighborhood.Id, false)
                    .Where(p => properties.ContainsKey(p.Prop_Id))
                    .OrderBy(p => p.SortValue).ToList();

                foreach (UplandForSaleProp prop in forSaleProps)
                {
                    UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(prop.Prop_Id);
                    Property localProperty = properties[prop.Prop_Id];

                    if (uplandProperty.status != Consts.PROP_STATUS_FORSALE)
                    {
                        Process_Single_SetForSale(localProperty, uplandProperty);
                    }
                    else
                    {
                        if (Process_Single_SetForSale_CleanSaleHistory(localProperty, uplandProperty))
                        {
                            okayProps++;
                        }
                        else
                        {
                            Process_Single_SetForSale(localProperty, uplandProperty);
                        }
                    }

                    if (okayProps == 10)
                    {
                        break;
                    }
                }
            }
        }

        private async Task Process_CollectionSaleResync(string collectionIds)
        {
            List<Collection> collections = new List<Collection>();
            if (collectionIds != "0")
            {
                collections = _localDataManager.GetCollections()
                    .Where(n => collectionIds.Split(",").Select(s => int.Parse(s)).ToList().Contains(n.Id))
                    .ToList();
            }
            else
            {
                collections = _localDataManager.GetCollections();
            }

            foreach (Collection collection in collections)
            {
                if (Consts.StandardCollectionIds.Contains(collection.Id) || collection.IsCityCollection)
                {
                    continue;
                }

                int okayProps = 0;
                List<UplandForSaleProp> forSaleProps = _localDataManager
                    .GetPropertiesForSale_Collection(collection.Id, false)
                    .Where(p => collection.MatchingPropertyIds.Contains(p.Prop_Id))
                    .OrderBy(p => p.SortValue).ToList();

                foreach (UplandForSaleProp prop in forSaleProps)
                {
                    UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(prop.Prop_Id);
                    Property localProperty = _localDataManager.GetProperty(prop.Prop_Id);

                    if (uplandProperty.status != Consts.PROP_STATUS_FORSALE)
                    {
                        Process_Single_SetForSale(localProperty, uplandProperty);
                    }
                    else
                    {
                        if (Process_Single_SetForSale_CleanSaleHistory(localProperty, uplandProperty))
                        {
                            okayProps++;
                        }
                        else
                        {
                            Process_Single_SetForSale(localProperty, uplandProperty);
                        }
                    }

                    if (okayProps == 10)
                    {
                        break;
                    }
                }
            }
        }

        private async Task Process_BuildingSaleResync()
        {
            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            foreach (int cityId in Consts.NON_BULLSHIT_CITY_IDS)
            {
                int okayProps = 0;

                Dictionary<long, Property> properties = _localDataManager
                    .GetPropertiesByCityId(cityId)
                    .ToDictionary(p => p.Id, p => p);

                List<UplandForSaleProp> forSaleProps = _localDataManager
                    .GetPropertiesForSale_City(cityId, true)
                    .Where(p => propertyStructures.ContainsKey(p.Prop_Id))
                    .OrderBy(p => p.SortValue).ToList();

                foreach (UplandForSaleProp prop in forSaleProps)
                {
                    UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(prop.Prop_Id);
                    Property localProperty = properties[prop.Prop_Id];

                    if (uplandProperty.status != Consts.PROP_STATUS_FORSALE)
                    {
                        Process_Single_SetForSale(localProperty, uplandProperty);
                    }
                    else
                    {
                        if (Process_Single_SetForSale_CleanSaleHistory(localProperty, uplandProperty))
                        {
                            okayProps++;
                        }
                        else
                        {
                            Process_Single_SetForSale(localProperty, uplandProperty);
                        }
                    }

                    if (okayProps == 100)
                    {
                        break;
                    }
                }
            }
        }

        private async Task Process_CityUnmintedResync(string cityIds)
        {
            List<int> ids = Consts.NON_BULLSHIT_CITY_IDS;

            foreach (int cityId in ids)
            {
                int okayProps = 0;

                List<Property> properties = _localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => p.Status == Consts.PROP_STATUS_UNLOCKED)
                    .OrderBy(p => p.Mint)
                    .ToList();

                if (cityIds == "-1")
                {
                    properties = properties.Where(p => !p.FSA).ToList();
                }

                foreach (Property localProperty in properties)
                {
                    UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(localProperty.Id);

                    if (uplandProperty.status == localProperty.Status)
                    {
                        okayProps++;
                    }
                    else
                    {
                        Process_Single_FullResync(localProperty, uplandProperty);
                    }

                    if (okayProps == 2500)
                    {
                        break;
                    }
                }
            }
        }

        private async Task Process_NeighborhoodUnmintedResync(string neighborhoodIds)
        {
            int maxOkay = 100;

            List<Neighborhood> neighborhoods = new List<Neighborhood>();
            if (neighborhoodIds != "0")
            {
                neighborhoods = _localDataManager.GetNeighborhoods()
                    .Where(n => neighborhoodIds.Split(",").Select(s => int.Parse(s)).ToList().Contains(n.Id))
                    .ToList();
                maxOkay = 1000;
            }
            else
            {
                neighborhoods = _localDataManager.GetNeighborhoods();
            }

            foreach (Neighborhood neighborhood in neighborhoods)
            {
                int okayProps = 0;

                List<Property> properties = _localDataManager
                    .GetPropertiesByCityId(neighborhood.CityId)
                        .Where(p => p.NeighborhoodId == neighborhood.Id && p.Status == Consts.PROP_STATUS_UNLOCKED)
                        .OrderBy(p => p.Mint)
                        .ToList();

                if (neighborhoodIds == "-1")
                {
                    properties = properties.Where(p => !p.FSA).ToList();
                }

                foreach (Property localProperty in properties)
                {
                    UplandProperty uplandProperty = await _uplandApiManager.GetUplandPropertyById(localProperty.Id);

                    if (uplandProperty.status == localProperty.Status)
                    {
                        okayProps++;
                    }
                    else
                    {
                        Process_Single_FullResync(localProperty, uplandProperty);
                    }

                    if (okayProps == maxOkay)
                    {
                        break;
                    }
                }
            }
        }

        private void Process_EnclaveFix()
        {
            Neighborhood boystown = _localDataManager.GetNeighborhoods().Where(n => n.Id == 295).First();
            Neighborhood lakeView = _localDataManager.GetNeighborhoods().Where(n => n.Id == 294).First();

            List<Property> lakeViewProperties = _localDataManager.GetPropertiesByCityId(5).Where(p => p.NeighborhoodId == lakeView.Id).ToList();

            foreach (Property prop in lakeViewProperties)
            {
                if (_localDataManager.IsPropertyInNeighborhood(boystown, prop))
                {
                    prop.NeighborhoodId = boystown.Id;
                    _localDataManager.UpsertProperty(prop);
                }
            }
        }
    }
}
