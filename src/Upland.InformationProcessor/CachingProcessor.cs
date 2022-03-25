using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Upland.Interfaces.Managers;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class CachingProcessor : ICachingProcessor
    {
        private readonly ILocalDataManager _localDataManager;

        private Dictionary<int, Tuple<DateTime, List<CachedForSaleProperty>>> _cityForSaleListCache;
        private Dictionary<int, Tuple<DateTime, List<CachedUnmintedProperty>>> _cityUnmintedCache;
        private Tuple<DateTime, Dictionary<long, string>> _propertyStructureCache;
        private Tuple<DateTime, bool> _isBlockchainUpdatesDisabledCache;
        private Tuple<DateTime, string> _latestAnnouncementString;
        private Tuple<DateTime, List<CollatedStatsObject>> _cityInfoCache;
        private Tuple<DateTime, List<CollatedStatsObject>> _neighborhoodInfoCache;
        private Tuple<DateTime, List<CollatedStatsObject>> _streetInfoCache;
        private Tuple<DateTime, List<CollatedStatsObject>> _collectionInfoCache;

        //NFTMetada Caches
        private bool _isLoadingNFTMetadataCache;
        private Tuple<DateTime, Dictionary<int, SpirithlwnMetadata>> _spiritHlwnMetadataCache;
        private Tuple<DateTime, Dictionary<int, StructornmtMetadata>> _structornmtMetadataCache;
        private Tuple<DateTime, Dictionary<int, BlockExplorerMetadata>> _blockExplorerMetadataCache;
        private Tuple<DateTime, Dictionary<int, EssentialMetadata>> _essentialMetadataCache;
        private Tuple<DateTime, Dictionary<int, MementoMetadata>> _mementoMetadataCache;

        private Dictionary<int, bool> _isLoadingCityForSaleListCache;
        private Dictionary<int, bool> _isLoadingCityUnmintedCache;
        private bool _isLoadingPropertyStructureCache;
        private bool _isLoadingCityInfoCache;
        private bool _isLoadingNeighborhoodInfoCache;
        private bool _isLoadingStreetInfoCache;
        private bool _isLoadingCollectionInfoCache;

        private readonly List<Tuple<int, HashSet<long>>> _collectionProperties;
        private readonly Dictionary<int, string> _neighborhoods;

        public CachingProcessor(ILocalDataManager localDataManager)
        {
            _localDataManager = localDataManager;

            InitializeCache();

            _collectionProperties = new List<Tuple<int, HashSet<long>>>();
            List<Tuple<int, long>> collectionProperties = _localDataManager.GetCollectionPropertyTable();

            _neighborhoods = new Dictionary<int, string>();
            _neighborhoods = _localDataManager.GetNeighborhoods().ToDictionary(n => n.Id, n => n.Name);

            foreach (int collectionId in collectionProperties.GroupBy(c => c.Item1).Select(g => g.First().Item1))
            {
                _collectionProperties.Add(new Tuple<int, HashSet<long>>(collectionId, collectionProperties.Where(c => c.Item1 == collectionId).Select(c => c.Item2).ToHashSet()));
            }
        }

        private void InitializeCache()
        {
            _isLoadingPropertyStructureCache = false;
            _isLoadingCityInfoCache = false;
            _isLoadingNeighborhoodInfoCache = false;
            _isLoadingStreetInfoCache = false;
            _isLoadingCollectionInfoCache = false;

            _propertyStructureCache = new Tuple<DateTime, Dictionary<long, string>>(DateTime.UtcNow.AddDays(-1), new Dictionary<long, string>());
            _isBlockchainUpdatesDisabledCache = new Tuple<DateTime, bool>(DateTime.UtcNow.AddDays(-1), false);
            _latestAnnouncementString = new Tuple<DateTime, string>(DateTime.UtcNow.AddDays(-1), "");
            _cityInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(DateTime.UtcNow.AddDays(-1), new List<CollatedStatsObject>());
            _neighborhoodInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(DateTime.UtcNow.AddDays(-1), new List<CollatedStatsObject>());
            _streetInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(DateTime.UtcNow.AddDays(-1), new List<CollatedStatsObject>());
            _collectionInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(DateTime.UtcNow.AddDays(-1), new List<CollatedStatsObject>());

            _isLoadingCityForSaleListCache = new Dictionary<int, bool>();
            _isLoadingCityUnmintedCache = new Dictionary<int, bool>();
            _cityForSaleListCache = new Dictionary<int, Tuple<DateTime, List<CachedForSaleProperty>>>();
            _cityUnmintedCache = new Dictionary<int, Tuple<DateTime, List<CachedUnmintedProperty>>>();

            foreach (int cityId in Consts.NON_BULLSHIT_CITY_IDS)
            {
                _isLoadingCityForSaleListCache.Add(cityId, false);
                _isLoadingCityUnmintedCache.Add(cityId, false);
                _cityForSaleListCache.Add(cityId, new Tuple<DateTime, List<CachedForSaleProperty>>(DateTime.UtcNow.AddDays(-1), new List<CachedForSaleProperty>()));
                _cityUnmintedCache.Add(cityId, new Tuple<DateTime, List<CachedUnmintedProperty>>(DateTime.UtcNow.AddDays(-1), new List<CachedUnmintedProperty>()));
            }

            _isLoadingNFTMetadataCache = false; ;
            _spiritHlwnMetadataCache = new Tuple<DateTime, Dictionary<int, SpirithlwnMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, SpirithlwnMetadata>());
            _structornmtMetadataCache = new Tuple<DateTime, Dictionary<int, StructornmtMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, StructornmtMetadata>());
            _blockExplorerMetadataCache = new Tuple<DateTime, Dictionary<int, BlockExplorerMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, BlockExplorerMetadata>());
            _essentialMetadataCache = new Tuple<DateTime, Dictionary<int, EssentialMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, EssentialMetadata>());
            _mementoMetadataCache = new Tuple<DateTime, Dictionary<int, MementoMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, MementoMetadata>());
        }

        public Dictionary<long, string> GetPropertyStructuresFromCache()
        {
            if (!_isLoadingPropertyStructureCache && _propertyStructureCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingPropertyStructureCache = true;
                _propertyStructureCache = new Tuple<DateTime, Dictionary<long, string>>(
                    DateTime.UtcNow.AddMinutes(30),
                    _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType));
                _isLoadingPropertyStructureCache = false;
            }

            return _propertyStructureCache.Item2;
        }

        public List<CachedForSaleProperty> GetCityForSaleListFromCache(int cityId)
        {
            if (!_isLoadingCityForSaleListCache[cityId] && _cityForSaleListCache[cityId].Item1 < DateTime.UtcNow)
            {
                _isLoadingCityForSaleListCache[cityId] = true;
                _cityForSaleListCache[cityId] = new Tuple<DateTime, List<CachedForSaleProperty>>(
                    DateTime.UtcNow.AddMinutes(5),
                   _localDataManager.GetCachedForSaleProperties(cityId));

                foreach (CachedForSaleProperty prop in _cityForSaleListCache[cityId].Item2)
                {
                    prop.CollectionIds = GetCollectionIdListForPropertyId(prop.Id);
                }

                _isLoadingCityForSaleListCache[cityId] = false;
            }

            RemoveExpiredSalesEntries();

            return _cityForSaleListCache[cityId].Item2;
        }

        public List<CachedUnmintedProperty> GetCityUnmintedFromCache(int cityId)
        {
            if (!_isLoadingCityUnmintedCache[cityId] && _cityUnmintedCache[cityId].Item1 < DateTime.UtcNow)
            {
                _isLoadingCityUnmintedCache[cityId] = true;
                _cityUnmintedCache[cityId] = new Tuple<DateTime, List<CachedUnmintedProperty>>(
                    DateTime.UtcNow.AddMinutes(5),
                   _localDataManager.GetCachedUnmintedProperties(cityId));

                foreach (CachedUnmintedProperty prop in _cityUnmintedCache[cityId].Item2)
                {
                    prop.CollectionIds = GetCollectionIdListForPropertyId(prop.Id);
                }

                _isLoadingCityUnmintedCache[cityId] = false;
            }

            RemoveExpiredUnmintedEntries();

            return _cityUnmintedCache[cityId].Item2;
        }

        public List<CollatedStatsObject> GetCityInfoFromCache()
        {
            if (!_isLoadingCityInfoCache && _cityInfoCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingCityInfoCache = true;
                _cityInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                    DateTime.UtcNow.AddMinutes(1),
                    _localDataManager.GetCityStats());
                _isLoadingCityInfoCache = false;
            }

            return _cityInfoCache.Item2;
        }

        public List<CollatedStatsObject> GetNeighborhoodInfoFromCache()
        {
            if (!_isLoadingNeighborhoodInfoCache && _neighborhoodInfoCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingNeighborhoodInfoCache = true;
                _neighborhoodInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                    DateTime.UtcNow.AddMinutes(1),
                    _localDataManager.GetNeighborhoodStats());
                _isLoadingNeighborhoodInfoCache = false;
            }

            return _neighborhoodInfoCache.Item2;
        }

        public List<CollatedStatsObject> GetStreetInfoFromCache()
        {
            if (!_isLoadingStreetInfoCache && _streetInfoCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingStreetInfoCache = true;
                _streetInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                    DateTime.UtcNow.AddMinutes(1),
                    _localDataManager.GetStreetStats());
                _isLoadingStreetInfoCache = false;
            }

            return _streetInfoCache.Item2;
        }

        public List<CollatedStatsObject> GetCollectionInfoFromCache()
        {
            if (!_isLoadingCollectionInfoCache && _collectionInfoCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingCollectionInfoCache = true;
                _collectionInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                    DateTime.UtcNow.AddMinutes(1),
                    _localDataManager.GetCollectionStats());
                _isLoadingCollectionInfoCache = false;
            }

            return _collectionInfoCache.Item2;
        }

        public Dictionary<int, SpirithlwnMetadata> GetSpiritHlwnMetadataFromCache()
        {
            if (!_isLoadingNFTMetadataCache && _spiritHlwnMetadataCache.Item1 < DateTime.UtcNow)
            {
                ReloadNFTMetaDataCache();
            }

            return _spiritHlwnMetadataCache.Item2;
        }

        public Dictionary<int, StructornmtMetadata> GetStructornmtMetadataFromCache()
        {
            if (!_isLoadingNFTMetadataCache && _structornmtMetadataCache.Item1 < DateTime.UtcNow)
            {
                ReloadNFTMetaDataCache();
            }

            return _structornmtMetadataCache.Item2;
        }

        public Dictionary<int, BlockExplorerMetadata> GetBlockExplorerMetadataFromCache()
        {
            if (!_isLoadingNFTMetadataCache && _blockExplorerMetadataCache.Item1 < DateTime.UtcNow)
            {
                ReloadNFTMetaDataCache();
            }

            return _blockExplorerMetadataCache.Item2;
        }

        public Dictionary<int, EssentialMetadata> GetEssentialMetadataFromCache()
        {
            if (!_isLoadingNFTMetadataCache && _essentialMetadataCache.Item1 < DateTime.UtcNow)
            {
                ReloadNFTMetaDataCache();
            }

            return _essentialMetadataCache.Item2;
        }

        public Dictionary<int, MementoMetadata> GetMementoMetadataFromCache()
        {
            if (!_isLoadingNFTMetadataCache && _mementoMetadataCache.Item1 < DateTime.UtcNow)
            {
                ReloadNFTMetaDataCache();
            }

            return _mementoMetadataCache.Item2;
        }

        private void ReloadNFTMetaDataCache()
        {
            if (_isLoadingNFTMetadataCache)
            {
                return;
            }

            _isLoadingNFTMetadataCache = true;

            List<NFTMetadata> newMetada = _localDataManager.GetAllNFTMetadata();

            _spiritHlwnMetadataCache = new Tuple<DateTime, Dictionary<int, SpirithlwnMetadata>>(
                DateTime.UtcNow.AddHours(1),
                newMetada.Where(m => m.Category == Consts.METADATA_TYPE_SPIRITHLWN)
                    .ToDictionary(m => m.Id, m => HelperFunctions.DecodeMetadata<SpirithlwnMetadata>(m.Metadata)));

            _structornmtMetadataCache = new Tuple<DateTime, Dictionary<int, StructornmtMetadata>>(
                DateTime.UtcNow.AddHours(1),
                newMetada.Where(m => m.Category == Consts.METADATA_TYPE_STRUCTORNMT)
                    .ToDictionary(m => m.Id, m => HelperFunctions.DecodeMetadata<StructornmtMetadata>(m.Metadata)));

            _blockExplorerMetadataCache = new Tuple<DateTime, Dictionary<int, BlockExplorerMetadata>>(
                DateTime.UtcNow.AddHours(1),
                newMetada.Where(m => m.Category == Consts.METADATA_TYPE_BLKEXPLORER)
                    .ToDictionary(m => m.Id, m => HelperFunctions.DecodeMetadata<BlockExplorerMetadata>(m.Metadata)));

            _essentialMetadataCache = new Tuple<DateTime, Dictionary<int, EssentialMetadata>>(
                DateTime.UtcNow.AddHours(1),
                newMetada.Where(m => m.Category == Consts.METADATA_TYPE_ESSENTIAL)
                    .ToDictionary(m => m.Id, m => HelperFunctions.DecodeMetadata<EssentialMetadata>(m.Metadata)));

            _mementoMetadataCache = new Tuple<DateTime, Dictionary<int, MementoMetadata>>(
                DateTime.UtcNow.AddHours(1),
                newMetada.Where(m => m.Category == Consts.METADATA_TYPE_MEMENTO)
                    .ToDictionary(m => m.Id, m => HelperFunctions.DecodeMetadata<MementoMetadata>(m.Metadata)));

            _isLoadingNFTMetadataCache = false;
        }

        public bool GetIsBlockchainUpdatesDisabledFromCache()
        {
            if (_isBlockchainUpdatesDisabledCache.Item1 < DateTime.UtcNow)
            {
                _isBlockchainUpdatesDisabledCache = new Tuple<DateTime, bool>(
                    DateTime.UtcNow.AddMinutes(1),
                    !bool.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES)));
            }

            return _isBlockchainUpdatesDisabledCache.Item2;
        }

        public string GetLatestAnnouncemenFromCache()
        {
            if (_latestAnnouncementString.Item1 < DateTime.UtcNow)
            {
                _latestAnnouncementString = new Tuple<DateTime, string>(
                    DateTime.UtcNow.AddMinutes(5),
                    _localDataManager.GetConfigurationValue(Consts.CONFIG_LATESTANNOUNCEMENT));
            }

            return _latestAnnouncementString.Item2;
        }

        public List<int> GetCollectionIdListForPropertyId(long propertyId)
        {
            List<int> collectionIds = new List<int>();

            foreach (Tuple<int, HashSet<long>> collectionProps in _collectionProperties)
            {
                if (collectionProps.Item2.Contains(propertyId))
                {
                    collectionIds.Add(collectionProps.Item1);
                }
            }

            return collectionIds;
        }

        private void RemoveExpiredSalesEntries()
        {
            foreach (int cityId in Consts.NON_BULLSHIT_CITY_IDS)
            {
                // Make sure its not loading, and the entry is expired, and that there is actually something to expire
                if (!_isLoadingCityForSaleListCache[cityId] && _cityForSaleListCache[cityId].Item1 < DateTime.UtcNow && _cityForSaleListCache[cityId].Item2.Count > 0)
                {
                    _isLoadingCityForSaleListCache[cityId] = true;

                    _cityForSaleListCache[cityId] = new Tuple<DateTime, List<CachedForSaleProperty>>(
                        DateTime.UtcNow.AddDays(-1),
                       new List<CachedForSaleProperty>());

                    _isLoadingCityForSaleListCache[cityId] = false;
                }
            }
        }

        private void RemoveExpiredUnmintedEntries()
        {
            foreach (int cityId in Consts.NON_BULLSHIT_CITY_IDS)
            {
                // Make sure its not loading, and the entry is expired, and that there is actually something to expire
                if (!_isLoadingCityUnmintedCache[cityId] && _cityUnmintedCache[cityId].Item1 < DateTime.UtcNow && _cityUnmintedCache[cityId].Item2.Count > 0)
                {
                    _isLoadingCityUnmintedCache[cityId] = true;

                    _cityUnmintedCache[cityId] = new Tuple<DateTime, List<CachedUnmintedProperty>>(
                        DateTime.UtcNow.AddDays(-1),
                       new List<CachedUnmintedProperty>());

                    _isLoadingCityUnmintedCache[cityId] = false;
                }
            }
        }
    }
}
