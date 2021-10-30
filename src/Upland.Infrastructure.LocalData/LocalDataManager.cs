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

        public LocalDataManager()
        {
            uplandApiRepository = new UplandApiRepository();
        }

        public async Task PopulateAllPropertiesInArea(double north, double south, double east, double west, int cityId, bool fullPropertyRetrieve)
        {
            List<long> retryIds = new List<long>();
            List<long> loadedProps = new List<long>();
            List<long> ignoreIds = new List<long>();
            Dictionary<long, Property> allCityProperties = new Dictionary<long, Property>();

            if (fullPropertyRetrieve)
            {
                LocalDataRepository.GetPropertiesByCityId(cityId).Where(p => p.Latitude.HasValue).Select(p => p.Id).ToList();
            }
            else
            {
                allCityProperties = LocalDataRepository.GetPropertiesByCityId(cityId).ToDictionary(p => p.Id, p => p);
            }

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
                            try
                            {
                                Property property = UplandMapper.Map(await uplandApiRepository.GetPropertyById(prop.Prop_Id));
                                LocalDataRepository.UpsertProperty(property);
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
                                List<Property> checkProps = LocalDataRepository.GetProperties(new List<long> { prop.Prop_Id });
                                if(checkProps.Count > 0)
                                {
                                    property = checkProps[0];
                                }
                                else
                                {
                                    property = UplandMapper.Map(await uplandApiRepository.GetPropertyById(prop.Prop_Id));
                                }
                            }
                            else
                            {
                                property = allCityProperties[prop.Prop_Id];
                            }

                            // Lets Just update the status and FSA
                            property.Status = prop.status;
                            property.FSA = prop.labels.fsa_allow;
                            LocalDataRepository.UpsertProperty(property);
                            loadedProps.Add(prop.Prop_Id);
                        }
                    }
                }
            }

            while (retryIds.Count > 0)
            {
                retryIds = await RetryPopulate(retryIds);
            }
        }

        private async Task<List<long>> RetryPopulate(List<long> retryIds)
        {
            List<long> nextRetryIds = new List<long>();

            foreach (long Id in retryIds)
            {
                try
                {
                    Property property = UplandMapper.Map(await uplandApiRepository.GetPropertyById(Id));
                    LocalDataRepository.UpsertProperty(property);
                }
                catch
                {
                    nextRetryIds.Add(Id);
                }
            }

            return nextRetryIds;
        }

        public async Task PopulateIndividualPropertyById(long propertyId)
        {
            Property property = UplandMapper.Map(await uplandApiRepository.GetPropertyById(propertyId));
            LocalDataRepository.UpsertProperty(property);
        }

        public async Task PopulateNeighborhoods()
        {
            List<Neighborhood> existingNeighborhoods = GetNeighborhoods();
            List<Neighborhood> neighborhoods = await uplandApiRepository.GetNeighborhoods();

            foreach(Neighborhood neighborhood in neighborhoods)
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

                    LocalDataRepository.CreateNeighborhood(neighborhood);
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
                    LocalDataRepository.CreateCollection(collection);
                }

                if (!Consts.StandardCollectionIds.Contains(collection.Id) && !collection.IsCityCollection)
                {
                    List<long> propIds = new List<long>();
                    propIds.AddRange((await uplandApiRepository.GetUnlockedNotForSaleCollectionProperties(collection.Id)).Select(p => p.Prop_Id));
                    propIds.AddRange((await uplandApiRepository.GetForSaleCollectionProperties(collection.Id)).Select(p => p.Prop_Id));
                    propIds.AddRange((await uplandApiRepository.GetMatchingCollectionsOwned(collection.Id)).Select(p => p.Prop_Id));

                    if (!existingCollections.Any(c => c.Id == collection.Id))
                    {
                        LocalDataRepository.CreateCollectionProperties(collection.Id, propIds);
                    }
                    else
                    {
                        List<long> newPropIds = propIds.Where(p => !existingCollections.Where(c => c.Id == collection.Id).First().MatchingPropertyIds.Contains(p)).ToList();
                        if (newPropIds.Count > 0)
                        {
                            LocalDataRepository.CreateCollectionProperties(collection.Id, newPropIds);
                        }
                    }
                }
            }
        }

        public async Task PopulationCollectionPropertyData(int collectionId)
        {
            Collection collection = GetCollections().Where(c => c.Id == collectionId).First();

            List<Property> existingCollectionProperties = GetPropertiesByCollectionId(collection.Id);

            foreach (long propId in collection.MatchingPropertyIds)
            {
                if (!existingCollectionProperties.Any(p => p.Id == propId))
                {
                    Property prop = UplandMapper.Map(await uplandApiRepository.GetPropertyById(propId));
                    LocalDataRepository.CreateProperty(prop);
                }
            }
        }

        public void DetermineNeighborhoodIdsForCity(int cityId)
        {
            List<Neighborhood> neighborhoods = GetNeighborhoods();
            List<Property> properties = LocalDataRepository.GetPropertiesByCityId(cityId);

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
                        LocalDataRepository.UpsertProperty(prop);
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
        
        public List<long> GetPropertyIdsByCollectionId(int collectionId)
        {
            return LocalDataRepository.GetCollectionPropertyIds(collectionId);
        }

        public List<Property> GetPropertiesByCityId(int cityId)
        {
            return LocalDataRepository.GetPropertiesByCityId(cityId);
        }

        public List<Property> GetPropertiesByCollectionId(int collectionId)
        {
            return LocalDataRepository.GetPropertiesByCollectionId(collectionId);
        }

        public List<Collection> GetCollections()
        {
            List<Collection> collections = LocalDataRepository.GetCollections();

            foreach (Collection collection in collections)
            {
                collection.MatchingPropertyIds = LocalDataRepository.GetCollectionPropertyIds(collection.Id);
            }

            return collections;
        }

        public async Task<List<Property>> GetPropertysByUsername(string username)
        {
            List<UplandAuthProperty> userPropIds = await uplandApiRepository.GetPropertysByUsername(username);

            List<Property> userProperties = LocalDataRepository.GetProperties(userPropIds.Select(p => p.Prop_Id).ToList());

            foreach (UplandAuthProperty propId in userPropIds)
            {
                if (!userProperties.Any(p => p.Id == propId.Prop_Id))
                {
                    Property prop = UplandMapper.Map(await uplandApiRepository.GetPropertyById(propId.Prop_Id));
                    LocalDataRepository.CreateProperty(prop);
                    userProperties.Add(prop);
                }
            }

            return userProperties;
        }

        public List<CollatedStatsObject> GetCityStats()
        {
            return CollateStats(LocalDataRepository.GetCityStats());
        }

        public List<CollatedStatsObject> GetNeighborhoodStats()
        {
            return CollateStats(LocalDataRepository.GetNeighborhoodStats());
        }

        public List<CollatedStatsObject> GetCollectionStats()
        {
            return CollateStats(LocalDataRepository.GetCollectionStats());
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

                switch(stat.Status)
                {
                    case "For Sale":
                        collatedStats.Last().ForSaleProps += stat.PropCount;
                        break;
                    case "Locked":
                        collatedStats.Last().LockedProps += stat.PropCount;
                        break;
                    case "Owned":
                        collatedStats.Last().OwnedProps += stat.PropCount;
                        break;
                    case "Unlocked":
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
            LocalDataRepository.CreateOptimizationRun(optimizationRun);
        }

        public void CreateNeighborhood(Neighborhood neighborhood)
        {
            LocalDataRepository.CreateNeighborhood(neighborhood);
        }

        public List<Neighborhood> GetNeighborhoods()
        {
            return LocalDataRepository.GetNeighborhoods();
        }

        public void SetOptimizationRunStatus(OptimizationRun optimizationRun)
        {
            LocalDataRepository.SetOptimizationRunStatus(optimizationRun);
        }

        public OptimizationRun GetLatestOptimizationRun(decimal discordUserId)
        {
            return LocalDataRepository.GetLatestOptimizationRun(discordUserId);
        }

        public RegisteredUser GetRegisteredUser(decimal discordUserId)
        {
            return LocalDataRepository.GetRegisteredUser(discordUserId);
        }

        public void CreateRegisteredUser(RegisteredUser registeredUser)
        {
            LocalDataRepository.CreateRegisteredUser(registeredUser);
        }

        public void IncreaseRegisteredUserRunCount(decimal discordUserId)
        {
            LocalDataRepository.IncreaseRegisteredUserRunCount(discordUserId);
        }

        public void DeleteRegisteredUser(decimal discordUserId)
        {
            LocalDataRepository.DeleteRegisteredUser(discordUserId);
        }

        public void DeleteOptimizerRuns(decimal discordUserId)
        {
            LocalDataRepository.DeleteOptimizerRuns(discordUserId);
        }

        public void SetRegisteredUserVerified(decimal discordUserId)
        {
            LocalDataRepository.SetRegisteredUserVerified(discordUserId);
        }

        public void SetRegisteredUserPaid(string uplandUsername)
        {
            LocalDataRepository.SetRegisteredUserPaid(uplandUsername);
        }

        public void TruncatePropertyStructure()
        {
            LocalDataRepository.TruncatePropertyStructure();
        }

        public void CreatePropertyStructure(PropertyStructure propertyStructure)
        {
            LocalDataRepository.CreatePropertyStructure(propertyStructure);
        }

        public List<PropertyStructure> GetPropertyStructures()
        {
            return LocalDataRepository.GetPropertyStructures();
        }
    }
}
