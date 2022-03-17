using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Upland.Infrastructure.UplandApi;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Repositories;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Infrastructure.LocalData
{
    public class LocalDataManager : ILocalDataManager
    {
        private IUplandApiRepository _uplandApiRepository;
        private ILocalDataRepository _localDataRepository;

        public LocalDataManager(IUplandApiRepository uplandApiRepository, ILocalDataRepository localDataRepository)
        {
            _uplandApiRepository = uplandApiRepository;
            _localDataRepository = localDataRepository;
        }

        public async Task PopulateAllPropertiesInArea(double north, double south, double east, double west, int cityId)
        {
            List<long> retryIds = new List<long>();
            List<long> loadedProps = new List<long>();
            Dictionary<long, Property> allCityProperties = new Dictionary<long, Property>();
            List<Neighborhood> neighborhoods = GetNeighborhoods();

            allCityProperties = _localDataRepository.GetPropertiesByCityId(cityId).Where(p => p.Latitude.HasValue).ToDictionary(p => p.Id, p => p);

            double defaultStep = 0.005;
            int totalprops = 0;

            for (double y = north; y > south - defaultStep; y -= defaultStep)
            {
                for (double x = west; x < east + defaultStep; x += defaultStep)
                {
                    List<UplandProperty> sectorProps = await _uplandApiRepository.GetPropertiesByArea(y, x, defaultStep);
                    totalprops += sectorProps.Count;
                    foreach (UplandProperty prop in sectorProps)
                    {
                        if (loadedProps.Contains(prop.Prop_Id))
                        {
                            continue;
                        }

                        // Check to see if the prop exists
                        if (!allCityProperties.ContainsKey(prop.Prop_Id))
                        {
                            // check to make sure it is not under another city if it is continue
                            List<Property> checkProps = _localDataRepository.GetProperties(new List<long> { prop.Prop_Id });
                            if (checkProps.Count > 0)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // if the property is in the city and has a non 0 mint continue
                            if (allCityProperties[prop.Prop_Id].Mint != 0)
                            {
                                continue;
                            }
                        }

                        // now lets populate it
                        try
                        {
                            Property property = UplandMapper.Map(await _uplandApiRepository.GetPropertyById(prop.Prop_Id));

                            // Due to blockchain updating, clear the owner and status
                            property.Owner = null;
                            property.MintedBy = null;
                            property.MintedOn = null;

                            if (property.Status != Consts.PROP_STATUS_LOCKED)
                            {
                                property.Status = Consts.PROP_STATUS_UNLOCKED;
                            }

                            property.NeighborhoodId = GetNeighborhoodIdForProp(neighborhoods, property);

                            _localDataRepository.UpsertProperty(property);
                            loadedProps.Add(prop.Prop_Id);
                        }
                        catch
                        {
                            retryIds.Add(prop.Prop_Id);
                        }
                    }
                }
            }

            while (retryIds.Count > 0)
            {
                retryIds = await RetryPopulate(retryIds, neighborhoods);
            }
        }

        public async Task ResetLockedProps(double north, double south, double east, double west, int cityId)
        {
            List<long> retryIds = new List<long>();
            List<long> loadedProps = new List<long>();
            Dictionary<long, Property> allCityProperties = new Dictionary<long, Property>();
            List<Neighborhood> neighborhoods = GetNeighborhoods();

            allCityProperties = _localDataRepository.GetPropertiesByCityId(cityId).Where(p => p.Latitude.HasValue).ToDictionary(p => p.Id, p => p);

            double defaultStep = 0.005;
            int totalprops = 0;

            for (double y = north; y > south - defaultStep; y -= defaultStep)
            {
                for (double x = west; x < east + defaultStep; x += defaultStep)
                {
                    List<UplandProperty> sectorProps = await _uplandApiRepository.GetPropertiesByArea(y, x, defaultStep);
                    totalprops += sectorProps.Count;
                    foreach (UplandProperty prop in sectorProps)
                    {
                        // skip loadedProps
                        if (loadedProps.Contains(prop.Prop_Id))
                        {
                            continue;
                        }

                        // We only care if the prop is in the Database
                        if (!allCityProperties.ContainsKey(prop.Prop_Id))
                        {
                            Property property = UplandMapper.Map(await _uplandApiRepository.GetPropertyById(prop.Prop_Id));
                            property.NeighborhoodId = GetNeighborhoodIdForProp(neighborhoods, property);

                            _localDataRepository.UpsertProperty(property);

                            continue;
                        }

                        if (prop.status == Consts.PROP_STATUS_LOCKED)
                        {
                            allCityProperties[prop.Prop_Id].Status = Consts.PROP_STATUS_LOCKED;
                            allCityProperties[prop.Prop_Id].Mint = 0;
                            allCityProperties[prop.Prop_Id].Owner = null;
                            allCityProperties[prop.Prop_Id].MintedBy = null;
                            allCityProperties[prop.Prop_Id].MintedOn = null;

                            _localDataRepository.UpsertProperty(allCityProperties[prop.Prop_Id]);
                            loadedProps.Add(prop.Prop_Id);
                        }
                    }
                }
            }
        }

        public int GetNeighborhoodIdForProp(List<Neighborhood> neighborhoods, Property property)
        {
            foreach (Neighborhood neighborhood in neighborhoods)
            {
                if (IsPropertyInNeighborhood(neighborhood, property))
                {
                    return neighborhood.Id;
                }
            }

            return -1;
        }

        private async Task<List<long>> RetryPopulate(List<long> retryIds, List<Neighborhood> neighborhoods)
        {
            List<long> nextRetryIds = new List<long>();

            foreach (long Id in retryIds)
            {
                try
                {
                    Property property = UplandMapper.Map(await _uplandApiRepository.GetPropertyById(Id));
                    property.NeighborhoodId = GetNeighborhoodIdForProp(neighborhoods, property);

                    _localDataRepository.UpsertProperty(property);
                }
                catch
                {
                    nextRetryIds.Add(Id);
                }
            }

            return nextRetryIds;
        }

        public async Task PopulateIndividualPropertyById(long propertyId, List<Neighborhood> neighborhoods)
        {
            Property property = UplandMapper.Map(await _uplandApiRepository.GetPropertyById(propertyId));
            property.NeighborhoodId = GetNeighborhoodIdForProp(neighborhoods, property);
            _localDataRepository.UpsertProperty(property);
        }

        public async Task PopulateNeighborhoods()
        {
            List<Neighborhood> existingNeighborhoods = GetNeighborhoods();
            List<Neighborhood> neighborhoods = await _uplandApiRepository.GetNeighborhoods();

            foreach (Neighborhood neighborhood in neighborhoods)
            {
                if (!existingNeighborhoods.Any(n => n.Id == neighborhood.Id))
                {
                    if (neighborhood.Boundaries == null)
                    {
                        neighborhood.Coordinates = new List<List<List<List<double>>>>();
                    }
                    else if (neighborhood.Boundaries.Type == "Polygon")
                    {
                        neighborhood.Coordinates = new List<List<List<List<double>>>>();
                        neighborhood.Coordinates.Add(JsonSerializer.Deserialize<List<List<List<double>>>>(neighborhood.Boundaries.Coordinates.ToString()));
                    }
                    else if (neighborhood.Boundaries.Type == "MultiPolygon")
                    {
                        neighborhood.Coordinates = JsonSerializer.Deserialize<List<List<List<List<double>>>>>(neighborhood.Boundaries.Coordinates.ToString());
                    }

                    _localDataRepository.CreateNeighborhood(neighborhood);
                }
            }
        }

        public async Task PopulateStreets()
        {
            List<Street> existingStreets = GetStreets();
            List<int> failedIds = new List<int>();

            for (int i = existingStreets.Max(s => s.Id); i <= Consts.MaxStreetNumber; i++)
            {
                if (!existingStreets.Any(s => s.Id == i))
                {
                    try
                    {
                        Street street = await _uplandApiRepository.GetStreet(i);
                        if (street.Name != "NotFound")
                        {
                            if (street.Type == null || street.Type == "null")
                            {
                                street.Type = "None";
                            }

                            _localDataRepository.CreateStreet(street);
                        }
                    }
                    catch
                    {
                        failedIds.Add(i);
                    }
                }
            }

            if (failedIds.Count > 0)
            {
                _localDataRepository.CreateErrorLog("LocalDataManager.cs - PopulateStreets", string.Join(", ", failedIds));
            }
        }

        public async Task PopulateDatabaseCollectionInfo()
        {
            List<Collection> collections = UplandMapper.Map((await _uplandApiRepository.GetCollections()).Where(c => c.Name != "Not Available").ToList());
            List<Collection> existingCollections = GetCollections();

            foreach (Collection collection in collections)
            {
                if (!existingCollections.Any(c => c.Id == collection.Id))
                {
                    _localDataRepository.CreateCollection(collection);
                }

                if (!Consts.StandardCollectionIds.Contains(collection.Id) && !collection.IsCityCollection)
                {
                    List<long> propIds = new List<long>();
                    propIds.AddRange((await _uplandApiRepository.GetUnlockedNotForSaleCollectionProperties(collection.Id)).Select(p => p.Prop_Id));
                    propIds.AddRange((await _uplandApiRepository.GetForSaleCollectionProperties(collection.Id)).Select(p => p.Prop_Id));
                    propIds.AddRange((await _uplandApiRepository.GetMatchingCollectionsOwned(collection.Id)).Select(p => p.Prop_Id));

                    if (!existingCollections.Any(c => c.Id == collection.Id))
                    {
                        _localDataRepository.CreateCollectionProperties(collection.Id, propIds);

                        await AdjustMintOnCollectionPropertys(collection, propIds);
                    }
                    else
                    {
                        List<long> newPropIds = propIds.Where(p => !existingCollections.Where(c => c.Id == collection.Id).First().MatchingPropertyIds.Contains(p)).ToList();
                        if (newPropIds.Count > 0)
                        {
                            _localDataRepository.CreateCollectionProperties(collection.Id, newPropIds);

                            if (collection.CityId == 33)
                            {
                                await AdjustMintOnCollectionPropertys(collection, newPropIds);
                            }
                        }
                    }
                }
            }
        }

        public void DetermineNeighborhoodIdsForCity(int cityId)
        {
            List<Neighborhood> neighborhoods = GetNeighborhoods();
            List<Property> properties = _localDataRepository.GetPropertiesByCityId(cityId);

            neighborhoods = neighborhoods.Where(n => n.CityId == cityId).ToList();
            properties = properties.Where(p => p.NeighborhoodId == null && p.Latitude != null).ToList();

            foreach (Property prop in properties)
            {
                if (prop.NeighborhoodId.HasValue)
                {
                    continue;
                }

                foreach (Neighborhood neighborhood in neighborhoods)
                {
                    if (IsPropertyInNeighborhood(neighborhood, prop))
                    {
                        prop.NeighborhoodId = neighborhood.Id;
                        _localDataRepository.UpsertProperty(prop);
                        break;
                    }
                }
            }
        }

        public bool IsPropertyInNeighborhood(Neighborhood neighborhood, Property property)
        {
            if (neighborhood.Coordinates.Count == 0)
            {
                return false;
            }

            foreach (List<List<List<double>>> part in neighborhood.Coordinates)
            {
                foreach (List<List<double>> polygon in part)
                {
                    if (IsPointInPolygon(polygon, property))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Stack Overflow Black Magic
        private bool IsPointInPolygon(List<List<double>> polygon, Property property)
        {
            int i, j = polygon.Count - 1;
            bool oddNodes = false;
            double y = (double)property.Latitude.Value;
            double x = (double)property.Longitude.Value;

            for (i = 0; i < polygon.Count; i++)
            {
                if ((polygon[i][1] < y && polygon[j][1] >= y
                || polygon[j][1] < y && polygon[i][1] >= y)
                && (polygon[i][0] <= x || polygon[j][0] <= x))
                {
                    oddNodes ^= (polygon[i][0] + (y - polygon[i][1]) / (polygon[j][1] - polygon[i][1]) * (polygon[j][0] - polygon[i][0]) < x);
                }
                j = i;
            }

            return oddNodes;
        }

        private async Task AdjustMintOnCollectionPropertys(Collection collection, List<long> propIds)
        {
            // If the collection is new and is limited or better adjust mint price
            if (collection.Category > 1)
            {
                List<Property> properties = _localDataRepository.GetProperties(propIds);
                foreach (Property prop in properties)
                {
                    bool retry = true;
                    while (retry)
                    {
                        try
                        {
                            Property property = UplandMapper.Map(await _uplandApiRepository.GetPropertyById(prop.Id));
                            prop.Mint = property.Mint;
                            _localDataRepository.UpsertProperty(prop);
                            retry = false;
                        }
                        catch
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
        }

        public List<CachedForSaleProperty> GetCachedForSaleProperties(int cityId)
        {
            return _localDataRepository.GetCachedForSaleProperties(cityId);
        }

        public List<CachedUnmintedProperty> GetCachedUnmintedProperties(int cityId)
        {
            List<CachedUnmintedProperty> unmintedProperties = new List<CachedUnmintedProperty>();
            List<Property> cityProperties = _localDataRepository.GetPropertiesByCityId(cityId)
                .Where(p => p.Status == Consts.PROP_STATUS_UNLOCKED && p.NeighborhoodId.HasValue)
                .ToList();

            foreach (Property property in cityProperties)
            {
                unmintedProperties.Add(new CachedUnmintedProperty
                {
                    Id = property.Id,
                    Address = property.Address,
                    CityId = property.CityId,
                    NeighborhoodId = property.NeighborhoodId.Value,
                    StreetId = property.StreetId,
                    Size = property.Size,
                    FSA = property.FSA,
                    Mint = property.Mint,
                    CollectionIds = new List<int>()
                });
            }

            return unmintedProperties;
        }

        public List<CachedSaleHistoryEntry> GetCachedSaleHistoryEntries(WebSaleHistoryFilters filters)
        {
            return _localDataRepository.GetCachedSaleHistoryEntries(filters);
        }

        public List<Tuple<string, double>> CatchWhales()
        {
            return _localDataRepository.CatchWhales();
        }

        public List<Tuple<int, long>> GetCollectionPropertyTable()
        {
            return _localDataRepository.GetCollectionPropertyTable();
        }

        public Property GetProperty(long id)
        {
            return _localDataRepository.GetProperty(id);
        }

        public List<Property> GetProperties(List<long> ids)
        {
            return _localDataRepository.GetProperties(ids);
        }

        public List<long> GetPropertyIdsByCollectionId(int collectionId)
        {
            return _localDataRepository.GetCollectionPropertyIds(collectionId);
        }

        public List<Property> GetPropertiesByUplandUsername(string uplandUsername)
        {
            return _localDataRepository.GetPropertiesByUplandUsername(uplandUsername);
        }

        public List<Property> GetPropertiesByCityId(int cityId)
        {
            return _localDataRepository.GetPropertiesByCityId(cityId);
        }

        public Property GetPropertyByCityIdAndAddress(int cityId, string address)
        {
            return _localDataRepository.GetPropertyByCityIdAndAddress(cityId, address);
        }

        public List<Property> GetPropertiesByCollectionId(int collectionId)
        {
            return _localDataRepository.GetPropertiesByCollectionId(collectionId);
        }

        public List<Collection> GetCollections()
        {
            List<Collection> collections = _localDataRepository.GetCollections();

            foreach (Collection collection in collections)
            {
                collection.MatchingPropertyIds = _localDataRepository.GetCollectionPropertyIds(collection.Id);
            }

            return collections;
        }

        public async Task<List<Property>> GetPropertysByUsername(string username)
        {
            List<UplandAuthProperty> userPropIds = await _uplandApiRepository.GetPropertysByUsername(username);

            List<Property> userProperties = _localDataRepository.GetProperties(userPropIds.Select(p => p.Prop_Id).ToList());

            return userProperties;
        }

        public List<Street> SearchStreets(string name)
        {
            name = name.ToUpper();
            List<Street> streets = _localDataRepository.GetStreets();
            List<Street> matches = new List<Street>();

            foreach (Street street in streets)
            {
                if (string.Format("{0} {1}", street.Name, street.Type).ToUpper().Contains(name))
                {
                    matches.Add(street);
                }
            }

            return matches;
        }

        public List<Neighborhood> SearchNeighborhoods(string name)
        {
            name = name.ToUpper();
            List<Neighborhood> neighborhoods = _localDataRepository.GetNeighborhoods();
            List<Neighborhood> matches = new List<Neighborhood>();

            foreach (Neighborhood neighborhood in neighborhoods)
            {
                if (neighborhood.Name.ToUpper().Contains(name))
                {
                    matches.Add(neighborhood);
                }
            }

            return matches;
        }

        public List<Collection> SearchCollections(string name)
        {
            name = name.ToUpper();
            List<Collection> collections = _localDataRepository.GetCollections();
            List<Collection> matches = new List<Collection>();

            foreach (Collection collection in collections)
            {
                if (collection.Name.ToUpper().Contains(name))
                {
                    matches.Add(collection);
                }
            }

            return matches;
        }

        public List<PropertySearchEntry> SearchProperties(int cityId, string address)
        {
            return _localDataRepository.SearchProperties(cityId, address);
        }

        public void SetHistoricalCityStats(DateTime timeStamp)
        {
            List<CollatedStatsObject> cityStats = GetCityStats();

            foreach (CollatedStatsObject stat in cityStats)
            {
                if (stat.PercentMinted > 0)
                {
                    stat.TimeStamp = timeStamp;
                    _localDataRepository.CreateHistoricalCityStatus(stat);
                }
            }
        }

        public List<AcquiredInfo> GetAcquiredOnByPlayer(string UplandUsername)
        {
            return _localDataRepository.GetAcquiredInfoByUser(UplandUsername);
        }

        public List<CollatedStatsObject> GetCityStats()
        {
            List<CollatedStatsObject> stats = CollateStats(_localDataRepository.GetCityStats());

            foreach(CollatedStatsObject stat in stats)
            {
                stat.CityId = stat.Id;
                stat.Name = Consts.Cities[stat.Id];
            }

            return stats;
        }

        public List<CollatedStatsObject> GetNeighborhoodStats()
        {
            List<CollatedStatsObject> stats = CollateStats(_localDataRepository.GetNeighborhoodStats());
            Dictionary<int, Neighborhood> neighborhoods = _localDataRepository.GetNeighborhoods().ToDictionary(n => n.Id, n => n);

            foreach (CollatedStatsObject stat in stats)
            {
                stat.CityId = neighborhoods[stat.Id].CityId;
                stat.Name = neighborhoods[stat.Id].Name;
            }

            return stats;
        }

        public List<CollatedStatsObject> GetStreetStats()
        {
            List<CollatedStatsObject> stats = CollateStats(_localDataRepository.GetStreetStats());
            Dictionary<int, Street> streets = _localDataRepository.GetStreets().ToDictionary(s => s.Id, s => s);

            foreach (CollatedStatsObject stat in stats)
            {
                stat.CityId = streets[stat.Id].CityId;
                stat.Name = streets[stat.Id].Name;
                if (streets[stat.Id].type != "None")
                {
                    stat.Name += " " + streets[stat.Id].type;
                }
            }

            return stats;
        }

        public List<CollatedStatsObject> GetCollectionStats()
        {
            List<CollatedStatsObject> stats = CollateStats(_localDataRepository.GetCollectionStats());
            Dictionary<int, Collection> collections = _localDataRepository.GetCollections().ToDictionary(c => c.Id, c => c);

            foreach (CollatedStatsObject stat in stats)
            {
                stat.CityId = collections[stat.Id].CityId.Value;
                stat.Name = collections[stat.Id].Name;
            }

            return stats;
        }

        private List<CollatedStatsObject> CollateStats(List<StatsObject> rawStats)
        {
            rawStats = rawStats.OrderBy(o => o.Id).ToList();
            List<CollatedStatsObject> collatedStats = new List<CollatedStatsObject>();
            int id = -1;

            foreach (StatsObject stat in rawStats)
            {
                if (stat.Id != id)
                {
                    id = stat.Id;
                    collatedStats.Add(new CollatedStatsObject { Id = stat.Id });
                }

                switch (stat.Status)
                {
                    case Consts.PROP_STATUS_FORSALE:
                        collatedStats.Last().ForSaleProps += stat.PropCount;
                        break;
                    case Consts.PROP_STATUS_LOCKED:
                        collatedStats.Last().LockedProps += stat.PropCount;
                        break;
                    case Consts.PROP_STATUS_OWNED:
                        collatedStats.Last().OwnedProps += stat.PropCount;
                        break;
                    case Consts.PROP_STATUS_UNLOCKED:
                        if (stat.FSA)
                        {
                            collatedStats.Last().UnlockedFSAProps += stat.PropCount;
                        }
                        else
                        {
                            collatedStats.Last().UnlockedNonFSAProps += stat.PropCount;
                        }
                        break;
                }

                if (stat.IsBuilt)
                {
                    collatedStats.Last().BuildingCount += stat.PropCount;
                }
            }

            foreach (CollatedStatsObject collatedStat in collatedStats)
            {
                collatedStat.TotalProps = collatedStat.ForSaleProps + collatedStat.LockedProps + collatedStat.OwnedProps + collatedStat.UnlockedFSAProps + collatedStat.UnlockedNonFSAProps;
                if ((collatedStat.TotalProps - collatedStat.LockedProps) == 0)
                {
                    collatedStat.PercentMinted = 100.00;
                    collatedStat.PercentNonFSAMinted = 100.00;
                }
                else
                {
                    collatedStat.PercentMinted = 100.00 * (collatedStat.ForSaleProps + collatedStat.OwnedProps) / (collatedStat.TotalProps - collatedStat.LockedProps);
                    collatedStat.PercentNonFSAMinted = 100.00 * (collatedStat.ForSaleProps + collatedStat.OwnedProps + collatedStat.UnlockedFSAProps) / (collatedStat.TotalProps - collatedStat.LockedProps);
                    collatedStat.PercentBuilt = 100.00 * (1.0 * collatedStat.BuildingCount / collatedStat.TotalProps);
                }
            }

            return collatedStats;
        }

        public List<PropertyAppraisalData> GetPreviousSalesAppraisalData()
        {
            return _localDataRepository.GetPreviousSalesAppraisalData();
        }

        public List<PropertyAppraisalData> GetCurrentFloorAppraisalData()
        {
            return _localDataRepository.GetCurrentFloorAppraisalData();
        }

        public List<PropertyAppraisalData> GetCurrentMarkupFloorAppraisalData()
        {
            return _localDataRepository.GetCurrentMarkupFloorAppraisalData();
        }

        public List<Tuple<string, double>> GetBuildingAppraisalData()
        {
            return _localDataRepository.GetBuildingAppraisalData();
        }

        public void CreateOptimizationRun(OptimizationRun optimizationRun)
        {
            _localDataRepository.CreateOptimizationRun(optimizationRun);
        }

        public void CreateAppraisalRun(AppraisalRun appraisalRun)
        {
            _localDataRepository.CreateAppraisalRun(appraisalRun);
        }

        public void CreateNeighborhood(Neighborhood neighborhood)
        {
            _localDataRepository.CreateNeighborhood(neighborhood);
        }

        public void CreateStreet(Street street)
        {
            _localDataRepository.CreateStreet(street);
        }

        public void CreateErrorLog(string location, string message)
        {
            _localDataRepository.CreateErrorLog(location, message);
        }

        public List<Neighborhood> GetNeighborhoods()
        {
            return _localDataRepository.GetNeighborhoods();
        }

        public List<Street> GetStreets()
        {
            return _localDataRepository.GetStreets();
        }

        public void SetOptimizationRunStatus(OptimizationRun optimizationRun)
        {
            _localDataRepository.SetOptimizationRunStatus(optimizationRun);
        }

        public OptimizationRun GetLatestOptimizationRun(int id)
        {
            return _localDataRepository.GetLatestOptimizationRun(id);
        }

        public AppraisalRun GetLatestAppraisalRun(int id)
        {
            return _localDataRepository.GetLatestAppraisalRun(id);
        }

        public RegisteredUser GetRegisteredUser(decimal discordUserId)
        {
            return _localDataRepository.GetRegisteredUser(discordUserId);
        }

        public List<SaleHistoryEntry> GetRawSaleHistoryByPropertyId(long propertyId)
        {
            return _localDataRepository.GetRawSaleHistoryByPropertyId(propertyId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByCityId(int cityId)
        {
            return _localDataRepository.GetSaleHistoryByCityId(cityId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByNeighborhoodId(int neighborhoodId)
        {
            return _localDataRepository.GetSaleHistoryByNeighborhoodId(neighborhoodId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByCollectionId(int collectionId)
        {
            return _localDataRepository.GetSaleHistoryByCollectionId(collectionId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByStreetId(int streetId)
        {
            return _localDataRepository.GetSaleHistoryByStreetId(streetId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByPropertyId(long propertyId)
        {
            return _localDataRepository.GetSaleHistoryByPropertyId(propertyId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByBuyerUsername(string buyerUsername)
        {
            return _localDataRepository.GetSaleHistoryByBuyerUsername(buyerUsername);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryBySellerUsername(string sellerUsername)
        {
            return _localDataRepository.GetSaleHistoryBySellerUsername(sellerUsername);
        }

        public List<CollatedStatsObject> GetHistoricalCityStatsByCityId(int cityId)
        {
            return _localDataRepository.GetHistoricalCityStatusByCityId(cityId);
        }

        public List<UplandForSaleProp> GetPropertiesForSale_City(int cityId, bool onlyBuildings)
        {
            return _localDataRepository.GetPropertiesForSale_City(cityId, onlyBuildings);
        }

        public List<UplandForSaleProp> GetPropertiesForSale_Neighborhood(int neighborhoodId, bool onlyBuildings)
        {
            return _localDataRepository.GetPropertiesForSale_Neighborhood(neighborhoodId, onlyBuildings);
        }

        public List<UplandForSaleProp> GetPropertiesForSale_Street(int streetId, bool onlyBuildings)
        {
            return _localDataRepository.GetPropertiesForSale_Street(streetId, onlyBuildings);
        }

        public List<UplandForSaleProp> GetPropertiesForSale_Collection(int collectionId, bool onlyBuildings)
        {
            return _localDataRepository.GetPropertiesForSale_Collection(collectionId, onlyBuildings);
        }

        public List<UplandForSaleProp> GetPropertiesForSale_Seller(string uplandUsername, bool onlyBuildings)
        {
            return _localDataRepository.GetPropertiesForSale_Seller(uplandUsername, onlyBuildings);
        }

        public string GetConfigurationValue(string name)
        {
            return _localDataRepository.GetConfigurationValue(name);
        }

        public EOSUser GetUplandUsernameByEOSAccount(string eosAccount)
        {
            if (eosAccount != null)
            {
                return _localDataRepository.GetUplandUsernameByEOSAccount(eosAccount);
            }

            return null;
        }

        public EOSUser GetEOSAccountByUplandUsername(string uplandUsername)
        {
            return _localDataRepository.GetEOSAccountByUplandUsername(uplandUsername);
        }

        public List<Tuple<decimal, string, string>> GetRegisteredUsersEOSAccounts()
        {
            return _localDataRepository.GetRegisteredUsersEOSAccounts();
        }

        public RegisteredUser GetRegisteredUserByUplandUsername(string uplandUsername)
        {
            return _localDataRepository.GetRegisteredUserByUplandUsername(uplandUsername);
        }

        public DateTime GetLastHistoricalCityStatusDate()
        {
            return _localDataRepository.GetLastHistoricalCityStatusDate();
        }

        public DateTime GetLastSaleHistoryDateTime()
        {
            return _localDataRepository.GetLastSaleHistoryDateTime();
        }

        public void UpdateSaleHistoryVistorToUplander(string oldEOS, string newEOS)
        {
            _localDataRepository.UpdateSaleHistoryVistorToUplander(oldEOS, newEOS);
        }

        public void DeleteSaleHistoryByBuyerEOSAccount(string eosAccount)
        {
            _localDataRepository.DeleteSaleHistoryByBuyerEOS(eosAccount);
        }

        public void CreateRegisteredUser(RegisteredUser registeredUser)
        {
            _localDataRepository.CreateRegisteredUser(registeredUser);
        }

        public void UpdateRegisteredUser(RegisteredUser registeredUser)
        {
            _localDataRepository.UpdateRegisteredUser(registeredUser);
        }

        public void DeleteRegisteredUser(int id)
        {
            _localDataRepository.DeleteRegisteredUser(id);
        }

        public void DeleteEOSUser(string eosAccount)
        {
            _localDataRepository.DeleteEOSUser(eosAccount);
        }

        public void DeleteSaleHistoryById(int id)
        {
            _localDataRepository.DeleteSaleHistoryById(id);
        }

        public void DeleteSaleHistoryByPropertyId(long propertyId)
        {
            _localDataRepository.DeleteSaleHistoryByPropertyId(propertyId);
        }

        public void DeleteOptimizerRuns(int id)
        {
            _localDataRepository.DeleteOptimizerRuns(id);
        }

        public void DeleteAppraisalRuns(int id)
        {
            _localDataRepository.DeleteAppraisalRuns(id);
        }

        public void TruncatePropertyStructure()
        {
            _localDataRepository.TruncatePropertyStructure();
        }

        public void CreatePropertyStructure(PropertyStructure propertyStructure)
        {
            _localDataRepository.CreatePropertyStructure(propertyStructure);
        }

        public List<PropertyStructure> GetPropertyStructures()
        {
            return _localDataRepository.GetPropertyStructures();
        }

        public void UpsertEOSUser(EOSUser eOSUser)
        {
            _localDataRepository.UpsertEOSUser(eOSUser);
        }

        public void UpsertSaleHistory(SaleHistoryEntry saleHistory)
        {
            _localDataRepository.UpsertSaleHistory(saleHistory);
        }

        public void UpsertConfigurationValue(string name, string value)
        {
            _localDataRepository.UpsertConfigurationValue(name, value);
        }

        public void UpsertProperty(Property property)
        {
            _localDataRepository.UpsertProperty(property);
        }

        public void UpsertSparkStaking(SparkStaking sparkStaking)
        {
            _localDataRepository.UpsertSparkStaking(sparkStaking);
        }

        public List<SparkStaking> GetSparkStakingByEOSUserId(int eosUserId)
        {
            return _localDataRepository.GetSparkStakingByEOSUserId(eosUserId);
        }

        public void UpsertNft(NFT nft)
        {
            _localDataRepository.UpsertNft(nft);
        }

        public void UpsertNftMetadata(NFTMetadata nftMetadata)
        {
            _localDataRepository.UpsertNftMetadata(nftMetadata);
        }

        public void UpsertNftHistory(NFTHistory nftHistory)
        {
            _localDataRepository.UpsertNftHistory(nftHistory);
        }

        public NFT GetNftByDGoodId(int dGoodId)
        {
            return _localDataRepository.GetNftByDGoodId(dGoodId);
        }

        public NFTMetadata GetNftMetadataById(int id)
        {
            return _localDataRepository.GetNftMetadataById(id);
        }

        public NFTMetadata GetNftMetadataByNameAndCategory(string name, string category)
        {
            return _localDataRepository.GetNftMetadataByNameAndCategory(name, category);
        }

        public List<NFTHistory> GetNftHistoryByDGoodId(string dGoodId)
        {
            return _localDataRepository.GetNftHistoryByDGoodId(dGoodId);
        }
    }
}
