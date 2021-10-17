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

        public List<long> GetPropertyIdsByCollectionId(int collectionId)
        {
            return LocalDataRepository.GetCollectionPropertyIds(collectionId);
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
