using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using System.Linq;
using Upland.Types.UplandApiTypes;

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

                if (!Consts.StandardAndCityCollectionIds.Contains(collection.Id))
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
            List<UplandPropId> userPropIds = await uplandApiRepository.GetPropertyIdsByUsername(username);

            List<Property> userProperties = LocalDataRepository.GetProperties(userPropIds.Select(p => p.Prop_Id).ToList());

            foreach (UplandPropId propId in userPropIds)
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
    }
}
