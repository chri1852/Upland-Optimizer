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

            foreach (Collection collection in collections)
            {
                LocalDataRepository.CreateCollection(collection);

                if (!Consts.StandardCollectionIds.Contains(collection.Id) && !Consts.CityCollectionIds.Contains(collection.Id))
                {
                    List<long> propIds = new List<long>();
                    propIds.AddRange((await uplandApiRepository.GetUnlockedNotForSaleCollectionProperties(collection.Id)).Select(p => p.Prop_Id));
                    propIds.AddRange((await uplandApiRepository.GetForSaleCollectionProperties(collection.Id)).Select(p => p.Prop_Id));
                    propIds.AddRange((await uplandApiRepository.GetMatchingCollectionsOwned(collection.Id)).Select(p => p.Prop_Id));

                    LocalDataRepository.CreateCollectionProperties(collection.Id, propIds);
                }
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

        public OptimizationRun GetLatestOptimizationRun(long discordId)
        {
            return LocalDataRepository.GetLatestOptimizationRun(discordId);
        }

        public RegisteredUser GetRegisteredUser(decimal discordUserId)
        {
            return LocalDataRepository.GetRegisteredUser(discordUserId);
        }

        public void CreateRegisteredUser(RegisteredUser registeredUser)
        {
            LocalDataRepository.CreateRegisteredUser(registeredUser);
        }

        public void IncreaseRegisteredUserRunCount(string uplandUsername)
        {
            LocalDataRepository.IncreaseRegisteredUserRunCount(uplandUsername);
        }

        public void DeleteRegisteredUser(decimal discordUserId)
        {
            LocalDataRepository.DeleteRegisteredUser(discordUserId);
        }

        public void SetRegisteredUserVerified(string uplandUsername)
        {
            LocalDataRepository.SetRegisteredUserVerified(uplandUsername);
        }

        public void SetRegisteredUserPaid(string uplandUsername)
        {
            LocalDataRepository.SetRegisteredUserPaid(uplandUsername);
        }
    }
}
