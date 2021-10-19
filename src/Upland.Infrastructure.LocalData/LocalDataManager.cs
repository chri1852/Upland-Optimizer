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

        public async Task PopulateAllPropertiesInArea(double north, double south, double east, double west)
        {
            List<long> retryIds = new List<long>();
            List<long> loadedProps = new List<long>();

            #region /* IgnoreIds */
            List<long> ignoreIds = LocalDataRepository.GetPropertiesByCityId(10).Where(p => p.Latitude.HasValue).Select(p => p.Id).ToList();
            #endregion /* IgnoreIds */

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

            foreach(Property prop in properties)
            {
                foreach(Neighborhood neighborhood in neighborhoods)
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
            foreach (List<List<double>> polygon in neighborhood.Coordinates[0])
            {
                if (IsPointInPolygon(polygon, property))
                {
                    return true;
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
    }
}
