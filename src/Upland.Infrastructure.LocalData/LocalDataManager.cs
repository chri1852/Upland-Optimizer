using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using System.Linq;
using Upland.Types.UplandApiTypes;
using Upland.Types.Types;

namespace Upland.Infrastructure.LocalData
{
    public class LocalDataManager
    {
        private UplandApiRepository uplandApiRepository;

        public LocalDataManager()
        {
            uplandApiRepository = new UplandApiRepository();
        }

        public async Task PopulateDatabaseCollectionInfo()
        {
            List<Collection> collections = UplandMapper.Map(await uplandApiRepository.GetCollections());
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
                        LocalDataRepository.CreateCollectionProperties(collection.Id, propIds.Where(p => !collection.MatchingPropertyIds.Contains(p)).ToList());
                    }
                }
            }

            // Now lets populate the collection Properties in the property table
            existingCollections = GetCollections();
            foreach(Collection collection in existingCollections)
            {

            }
        }

        public List<long> GetPropertyIdsByCollectionId(int collectionId)
        {
            return LocalDataRepository.GetCollectionPropertyIds(collectionId);
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
