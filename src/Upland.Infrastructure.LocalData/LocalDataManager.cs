using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using System.Linq;
using Upland.Types.UplandApiTypes;
using Upland.Types.Types;
using System.Text.Json;

namespace Upland.Infrastructure.LocalData
{
    public class LocalDataManager
    {
        private UplandApiRepository uplandApiRepository;
        private LocalDataRepository localDataRepository;

        public LocalDataManager()
        {
            uplandApiRepository = new UplandApiRepository();
            localDataRepository = new LocalDataRepository();
        }

        public async Task PopulateAllPropertiesInArea(double north, double south, double east, double west, int cityId, bool fullPropertyRetrieve)
        {
            List<long> retryIds = new List<long>();
            List<long> loadedProps = new List<long>();
            List<long> ignoreIds = new List<long>();
            Dictionary<long, Property> allCityProperties = new Dictionary<long, Property>();
            List<Neighborhood> neighborhoods = GetNeighborhoods();

            allCityProperties = localDataRepository.GetPropertiesByCityId(cityId).Where(p => p.Latitude.HasValue).ToDictionary(p => p.Id, p => p);

            double defaultStep = 0.005;
            int totalprops = 0;

            for (double y = north; y > south - defaultStep; y -= defaultStep)
            {
                for (double x = west; x < east + defaultStep; x += defaultStep)
                {
                    List<UplandProperty> sectorProps = await uplandApiRepository.GetPropertiesByArea(y, x, defaultStep);
                    totalprops += sectorProps.Count;
                    foreach (UplandProperty prop in sectorProps)
                    {
                        if (loadedProps.Contains(prop.Prop_Id) || ignoreIds.Contains(prop.Prop_Id))
                        {
                            continue;
                        }

                        // Get the full property Data
                        if (fullPropertyRetrieve)
                        {
                            // Check to see if the prop exists
                            if (!allCityProperties.ContainsKey(prop.Prop_Id))
                            {
                                // check to make sure it is not under another city if it is continue
                                List<Property> checkProps = localDataRepository.GetProperties(new List<long> { prop.Prop_Id });
                                if (checkProps.Count > 0)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                // if the property is in the city and has a non 0 mint continue
                                if (allCityProperties[prop.Prop_Id].MonthlyEarnings != 0)
                                {
                                    continue;
                                }
                            }

                            // now lets populate it
                            try
                            {
                                Property property = UplandMapper.Map(await uplandApiRepository.GetPropertyById(prop.Prop_Id));
                                property.NeighborhoodId = GetNeighborhoodIdForProp(neighborhoods, property);

                                localDataRepository.UpsertProperty(property);
                                loadedProps.Add(prop.Prop_Id);
                            }
                            catch
                            {
                                retryIds.Add(prop.Prop_Id);
                            }
                        }
                        else
                        {
                            Property property;

                            // Check to see if the prop exists, otherwise lets try and grab it.
                            if (!allCityProperties.ContainsKey(prop.Prop_Id))
                            {
                                // check to make sure it is not under another city
                                List<Property> checkProps = localDataRepository.GetProperties(new List<long> { prop.Prop_Id });
                                if (checkProps.Count > 0)
                                {
                                    property = checkProps[0];
                                }
                                else
                                {
                                    property = UplandMapper.Map(await uplandApiRepository.GetPropertyById(prop.Prop_Id));
                                    property.NeighborhoodId = GetNeighborhoodIdForProp(neighborhoods, property);
                                }
                            }
                            else
                            {
                                property = allCityProperties[prop.Prop_Id];
                            }

                            bool hasChanges = false;

                            if (property.MonthlyEarnings == 0 && prop.status != Consts.PROP_STATUS_LOCKED)
                            {
                                Property uplandProp = UplandMapper.Map(await uplandApiRepository.GetPropertyById(prop.Prop_Id));
                                property.MonthlyEarnings = uplandProp.MonthlyEarnings;
                                hasChanges = true;
                            }

                            if (property.Status != prop.status)
                            {
                                // Lock a prop
                                if (property.Status != Consts.PROP_STATUS_LOCKED && prop.status == Consts.PROP_STATUS_LOCKED)
                                {
                                    // Lets Just update the status and FSA
                                    property.Status = prop.status;
                                    property.FSA = prop.labels.fsa_allow;
                                    property.Owner = null;
                                    hasChanges = true;
                                    localDataRepository.DeleteSaleHistoryByPropertyId(property.Id);
                                }

                                // unlock a prop
                                if (property.Status == Consts.PROP_STATUS_LOCKED)
                                {
                                    Property uplandProp = UplandMapper.Map(await uplandApiRepository.GetPropertyById(prop.Prop_Id));
                                    property.Status = uplandProp.Status;
                                    property.MonthlyEarnings = uplandProp.MonthlyEarnings;
                                    property.Owner = uplandProp.Owner;
                                    hasChanges = true;
                                }

                                if (property.Status == Consts.PROP_STATUS_UNLOCKED)
                                {
                                    Property uplandProp = UplandMapper.Map(await uplandApiRepository.GetPropertyById(prop.Prop_Id));
                                    property.Status = prop.status;
                                    property.Owner = uplandProp.Owner;
                                    hasChanges = true;
                                }

                                if (property.Status == Consts.PROP_STATUS_FORSALE)
                                {
                                    Property uplandProp = UplandMapper.Map(await uplandApiRepository.GetPropertyById(prop.Prop_Id));
                                    property.Status = prop.status;
                                    property.Owner = uplandProp.Owner;
                                    localDataRepository.DeleteSaleHistoryByPropertyId(property.Id);
                                    hasChanges = true;
                                }
                            }

                            if (hasChanges)
                            {
                                localDataRepository.UpsertProperty(property);
                            }

                            loadedProps.Add(property.Id);
                        }
                    }
                }
            }

            while (retryIds.Count > 0)
            {
                retryIds = await RetryPopulate(retryIds, neighborhoods);
            }
        }

        private int GetNeighborhoodIdForProp(List<Neighborhood> neighborhoods, Property property)
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
                    Property property = UplandMapper.Map(await uplandApiRepository.GetPropertyById(Id));
                    property.NeighborhoodId = GetNeighborhoodIdForProp(neighborhoods, property);

                    localDataRepository.UpsertProperty(property);
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
            Property property = UplandMapper.Map(await uplandApiRepository.GetPropertyById(propertyId));
            property.NeighborhoodId = GetNeighborhoodIdForProp(neighborhoods, property);
            localDataRepository.UpsertProperty(property);
        }

        public async Task PopulateNeighborhoods()
        {
            List<Neighborhood> existingNeighborhoods = GetNeighborhoods();
            List<Neighborhood> neighborhoods = await uplandApiRepository.GetNeighborhoods();

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

                    localDataRepository.CreateNeighborhood(neighborhood);
                }
            }
        }

        public async Task PopulateStreets()
        {
            List<Street> existingStreets = GetStreets();
            List<int> failedIds = new List<int>();

            for (int i = 1; i <= Consts.MaxStreetNumber; i++)
            {
                if (!existingStreets.Any(s => s.Id == i))
                {
                    try
                    {
                        Street street = await uplandApiRepository.GetStreet(i);
                        if (street.Name != "NotFound")
                        {
                            if (street.Type == null || street.Type == "null")
                            {
                                street.Type = "None";
                            }

                            localDataRepository.CreateStreet(street);
                        }
                    }
                    catch
                    {
                        failedIds.Add(i);
                    }
                }
            }
        }

        public async Task PopulateDatabaseCollectionInfo()
        {
            List<Collection> collections = UplandMapper.Map((await uplandApiRepository.GetCollections()).Where(c => c.Name != "Not Available").ToList());
            List<Collection> existingCollections = GetCollections();

            foreach (Collection collection in collections)
            {
                if (!existingCollections.Any(c => c.Id == collection.Id))
                {
                    localDataRepository.CreateCollection(collection);
                }

                if (!Consts.StandardCollectionIds.Contains(collection.Id) && !collection.IsCityCollection)
                {
                    List<long> propIds = new List<long>();
                    propIds.AddRange((await uplandApiRepository.GetUnlockedNotForSaleCollectionProperties(collection.Id)).Select(p => p.Prop_Id));
                    propIds.AddRange((await uplandApiRepository.GetForSaleCollectionProperties(collection.Id)).Select(p => p.Prop_Id));
                    propIds.AddRange((await uplandApiRepository.GetMatchingCollectionsOwned(collection.Id)).Select(p => p.Prop_Id));

                    if (!existingCollections.Any(c => c.Id == collection.Id))
                    {
                        localDataRepository.CreateCollectionProperties(collection.Id, propIds);
                    }
                    else
                    {
                        List<long> newPropIds = propIds.Where(p => !existingCollections.Where(c => c.Id == collection.Id).First().MatchingPropertyIds.Contains(p)).ToList();
                        if (newPropIds.Count > 0)
                        {
                            localDataRepository.CreateCollectionProperties(collection.Id, newPropIds);
                        }
                    }
                }
            }
        }

        public void DetermineNeighborhoodIdsForCity(int cityId)
        {
            List<Neighborhood> neighborhoods = GetNeighborhoods();
            List<Property> properties = localDataRepository.GetPropertiesByCityId(cityId);

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
                        localDataRepository.UpsertProperty(prop);
                        break;
                    }
                }
            }
        }

        private bool IsPropertyInNeighborhood(Neighborhood neighborhood, Property property)
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

        public Property GetProperty(long id)
        {
            return localDataRepository.GetProperty(id);
        }

        public List<Property> GetProperties(List<long> ids)
        {
            return localDataRepository.GetProperties(ids);
        }


        public List<long> GetPropertyIdsByCollectionId(int collectionId)
        {
            return localDataRepository.GetCollectionPropertyIds(collectionId);
        }

        public List<Property> GetPropertiesByUplandUsername(string uplandUsername)
        {
            return localDataRepository.GetPropertiesByUplandUsername(uplandUsername);
        }

        public List<Property> GetPropertiesByCityId(int cityId)
        {
            return localDataRepository.GetPropertiesByCityId(cityId);
        }

        public Property GetPropertyByCityIdAndAddress(int cityId, string address)
        {
            return localDataRepository.GetPropertyByCityIdAndAddress(cityId, address);
        }

        public List<Property> GetPropertiesByCollectionId(int collectionId)
        {
            return localDataRepository.GetPropertiesByCollectionId(collectionId);
        }

        public List<Collection> GetCollections()
        {
            List<Collection> collections = localDataRepository.GetCollections();

            foreach (Collection collection in collections)
            {
                collection.MatchingPropertyIds = localDataRepository.GetCollectionPropertyIds(collection.Id);
            }

            return collections;
        }

        public async Task<List<Property>> GetPropertysByUsername(string username)
        {
            List<UplandAuthProperty> userPropIds = await uplandApiRepository.GetPropertysByUsername(username);

            List<Property> userProperties = localDataRepository.GetProperties(userPropIds.Select(p => p.Prop_Id).ToList());

            return userProperties;
        }

        public List<Street> SearchStreets(string name)
        {
            name = name.ToUpper();
            List<Street> streets = localDataRepository.GetStreets();
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

        public void SetHistoricalCityStats(DateTime timeStamp)
        {
            List<CollatedStatsObject> cityStats = GetCityStats();

            foreach (CollatedStatsObject stat in cityStats)
            {
                if (stat.PercentMinted > 0)
                {
                    stat.TimeStamp = timeStamp;
                    localDataRepository.CreateHistoricalCityStatus(stat);
                }
            }
        }

        public List<CollatedStatsObject> GetCityStats()
        {
            return CollateStats(localDataRepository.GetCityStats());
        }

        public List<CollatedStatsObject> GetNeighborhoodStats()
        {
            return CollateStats(localDataRepository.GetNeighborhoodStats());
        }

        public List<CollatedStatsObject> GetStreetStats()
        {
            return CollateStats(localDataRepository.GetStreetStats());
        }

        public List<CollatedStatsObject> GetCollectionStats()
        {
            return CollateStats(localDataRepository.GetCollectionStats());
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
                }
            }

            return collatedStats;
        }

        public void CreateOptimizationRun(OptimizationRun optimizationRun)
        {
            localDataRepository.CreateOptimizationRun(optimizationRun);
        }

        public void CreateNeighborhood(Neighborhood neighborhood)
        {
            localDataRepository.CreateNeighborhood(neighborhood);
        }

        public void CreateStreet(Street street)
        {
            localDataRepository.CreateStreet(street);
        }

        public List<Neighborhood> GetNeighborhoods()
        {
            return localDataRepository.GetNeighborhoods();
        }

        public List<Street> GetStreets()
        {
            return localDataRepository.GetStreets();
        }

        public void SetOptimizationRunStatus(OptimizationRun optimizationRun)
        {
            localDataRepository.SetOptimizationRunStatus(optimizationRun);
        }

        public OptimizationRun GetLatestOptimizationRun(decimal discordUserId)
        {
            return localDataRepository.GetLatestOptimizationRun(discordUserId);
        }

        public RegisteredUser GetRegisteredUser(decimal discordUserId)
        {
            return localDataRepository.GetRegisteredUser(discordUserId);
        }

        public List<SaleHistoryEntry> GetRawSaleHistoryByPropertyId(long propertyId)
        {
            return localDataRepository.GetRawSaleHistoryByPropertyId(propertyId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByCityId(int cityId)
        {
            return localDataRepository.GetSaleHistoryByCityId(cityId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByNeighborhoodId(int neighborhoodId)
        {
            return localDataRepository.GetSaleHistoryByNeighborhoodId(neighborhoodId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByCollectionId(int collectionId)
        {
            return localDataRepository.GetSaleHistoryByCollectionId(collectionId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByStreetId(int streetId)
        {
            return localDataRepository.GetSaleHistoryByStreetId(streetId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByPropertyId(long propertyId)
        {
            return localDataRepository.GetSaleHistoryByPropertyId(propertyId);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryByBuyerUsername(string buyerUsername)
        {
            return localDataRepository.GetSaleHistoryByBuyerUsername(buyerUsername);
        }

        public List<SaleHistoryQueryEntry> GetSaleHistoryBySellerUsername(string sellerUsername)
        {
            return localDataRepository.GetSaleHistoryBySellerUsername(sellerUsername);
        }

        public List<CollatedStatsObject> GetHistoricalCityStatsByCityId(int cityId)
        {
            return localDataRepository.GetHistoricalCityStatusByCityId(cityId);
        }

        public List<UplandForSaleProp> GetCityPropertiesForSale(int cityId)
        {
            return localDataRepository.GetCityPropertiesForSale(cityId);
        }

        public string GetConfigurationValue(string name)
        {
            return localDataRepository.GetConfigurationValue(name);
        }

        public Tuple<string, string> GetUplandUsernameByEOSAccount(string eosAccount)
        {
            return localDataRepository.GetUplandUserNameByEOSAccount(eosAccount);
        }

        public DateTime GetLastHistoricalCityStatusDate()
        {
            return localDataRepository.GetLastHistoricalCityStatusDate();
        }

        public DateTime GetLastSaleHistoryDateTime()
        {
            return localDataRepository.GetLastSaleHistoryDateTime();
        }

        public void UpdateSaleHistoryVistorToUplander(string oldEOS, string newEOS)
        {
            localDataRepository.UpdateSaleHistoryVistorToUplander(oldEOS, newEOS);
        }

        public void DeleteSaleHistoryByBuyerEOSAccount(string eosAccount)
        {
            localDataRepository.DeleteSaleHistoryByBuyerEOS(eosAccount);
        }

        public void CreateRegisteredUser(RegisteredUser registeredUser)
        {
            localDataRepository.CreateRegisteredUser(registeredUser);
        }

        public void IncreaseRegisteredUserRunCount(decimal discordUserId)
        {
            localDataRepository.IncreaseRegisteredUserRunCount(discordUserId);
        }

        public void DeleteRegisteredUser(decimal discordUserId)
        {
            localDataRepository.DeleteRegisteredUser(discordUserId);
        }

        public void DeleteEOSUser(string eosAccount)
        {
            localDataRepository.DeleteEOSUser(eosAccount);
        }

        public void DeleteSaleHistoryById(int id)
        {
            localDataRepository.DeleteSaleHistoryById(id);
        }

        public void DeleteSaleHistoryByPropertyId(long propertyId)
        {
            localDataRepository.DeleteSaleHistoryByPropertyId(propertyId);
        }

        public void DeleteOptimizerRuns(decimal discordUserId)
        {
            localDataRepository.DeleteOptimizerRuns(discordUserId);
        }

        public void SetRegisteredUserVerified(decimal discordUserId)
        {
            localDataRepository.SetRegisteredUserVerified(discordUserId);
        }

        public void SetRegisteredUserPaid(string uplandUsername)
        {
            localDataRepository.SetRegisteredUserPaid(uplandUsername);
        }

        public void TruncatePropertyStructure()
        {
            localDataRepository.TruncatePropertyStructure();
        }

        public void CreatePropertyStructure(PropertyStructure propertyStructure)
        {
            localDataRepository.CreatePropertyStructure(propertyStructure);
        }

        public List<PropertyStructure> GetPropertyStructures()
        {
            return localDataRepository.GetPropertyStructures();
        }

        public void UpsertEOSUser(string eosAccount, string uplandUsername, DateTime joined)
        {
            localDataRepository.UpsertEOSUser(eosAccount, uplandUsername, joined);
        }

        public void UpsertSaleHistory(SaleHistoryEntry saleHistory)
        {
            localDataRepository.UpsertSaleHistory(saleHistory);
        }

        public void UpsertConfigurationValue(string name, string value)
        {
            localDataRepository.UpsertConfigurationValue(name, value);
        }

        public void UpsertProperty(Property property)
        {
            localDataRepository.UpsertProperty(property);
        }
    }
}
