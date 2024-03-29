﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upland.Interfaces.Managers;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class CachingProcessor : ICachingProcessor
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IUplandApiManager _uplandApiManager;

        private Dictionary<int, Tuple<DateTime, List<CachedForSaleProperty>>> _cityForSaleListCache;
        private Dictionary<int, Tuple<DateTime, List<CachedUnmintedProperty>>> _cityUnmintedCache;
        private Tuple<DateTime, Dictionary<long, string>> _propertyStructureCache;
        private Tuple<DateTime, bool> _isBlockchainUpdatesDisabledCache;
        private Tuple<DateTime, bool> _uplandMaintenanceStatusCache;
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
        private Tuple<DateTime, Dictionary<int, StructureMetadata>> _structureMetadataCache;
        private Tuple<DateTime, Dictionary<int, LandVehicleMetadata>> _landvehicleMetadataCache;
        private Tuple<DateTime, Dictionary<int, int>> _nftCountsCache;

        private Dictionary<int, bool> _isLoadingCityForSaleListCache;
        private Dictionary<int, bool> _isLoadingCityUnmintedCache;
        private bool _isLoadingPropertyStructureCache;
        private bool _isLoadingCityInfoCache;
        private bool _isLoadingNeighborhoodInfoCache;
        private bool _isLoadingStreetInfoCache;
        private bool _isLoadingCollectionInfoCache;

        private readonly List<Tuple<int, HashSet<long>>> _collectionProperties;
        private readonly Dictionary<int, string> _neighborhoods;
        private readonly Dictionary<int, City> _cities;

        public CachingProcessor(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;

            InitializeCache();

            _collectionProperties = new List<Tuple<int, HashSet<long>>>();
            List<Tuple<int, long>> collectionProperties = _localDataManager.GetCollectionPropertyTable();

            _neighborhoods = new Dictionary<int, string>();
            _neighborhoods = _localDataManager.GetNeighborhoods().ToDictionary(n => n.Id, n => n.Name);

            _cities = new Dictionary<int, City>();
            _cities = _localDataManager.GetCities().ToDictionary(c => c.CityId, c => c);

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
            _uplandMaintenanceStatusCache = new Tuple<DateTime, bool>(DateTime.UtcNow.AddDays(-1), false);
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

            _isLoadingNFTMetadataCache = false;
            _spiritHlwnMetadataCache = new Tuple<DateTime, Dictionary<int, SpirithlwnMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, SpirithlwnMetadata>());
            _structornmtMetadataCache = new Tuple<DateTime, Dictionary<int, StructornmtMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, StructornmtMetadata>());
            _blockExplorerMetadataCache = new Tuple<DateTime, Dictionary<int, BlockExplorerMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, BlockExplorerMetadata>());
            _essentialMetadataCache = new Tuple<DateTime, Dictionary<int, EssentialMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, EssentialMetadata>());
            _mementoMetadataCache = new Tuple<DateTime, Dictionary<int, MementoMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, MementoMetadata>());
            _structureMetadataCache = new Tuple<DateTime, Dictionary<int, StructureMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, StructureMetadata>());
            _landvehicleMetadataCache = new Tuple<DateTime, Dictionary<int, LandVehicleMetadata>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, LandVehicleMetadata>());
            _nftCountsCache = new Tuple<DateTime, Dictionary<int, int>>(DateTime.UtcNow.AddDays(-1), new Dictionary<int, int>());
        }

        public Dictionary<long, string> GetPropertyStructuresFromCache()
        {
            if (!_isLoadingPropertyStructureCache && _propertyStructureCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingPropertyStructureCache = true;

                _propertyStructureCache = new Tuple<DateTime, Dictionary<long, string>>(
                    DateTime.UtcNow.AddMinutes(10),
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
                bool setAsExpired = false;

                try
                {
                    _cityForSaleListCache[cityId] = new Tuple<DateTime, List<CachedForSaleProperty>>(
                        DateTime.UtcNow.AddMinutes(5),
                       _localDataManager.GetCachedForSaleProperties(cityId));
                }
                catch
                {
                    setAsExpired = true;
                }

                if (setAsExpired || _cityForSaleListCache[cityId].Item2.Count == 0)
                {
                    _cityForSaleListCache[cityId] = new Tuple<DateTime, List<CachedForSaleProperty>>(
                        DateTime.UtcNow.AddMinutes(-5),
                        new List<CachedForSaleProperty>());
                }

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
                bool setAsExpired = false;

                try
                {
                    _cityUnmintedCache[cityId] = new Tuple<DateTime, List<CachedUnmintedProperty>>(
                        DateTime.UtcNow.AddMinutes(5),
                       _localDataManager.GetCachedUnmintedProperties(cityId));
                }
                catch
                {
                    setAsExpired = true;
                }

                if (setAsExpired || _cityUnmintedCache[cityId].Item2.Count == 0)
                {
                    _cityUnmintedCache[cityId] = new Tuple<DateTime, List<CachedUnmintedProperty>>(
                        DateTime.UtcNow.AddMinutes(-5),
                        new List<CachedUnmintedProperty>());
                }

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
                bool setAsExpired = false;

                try
                {
                    _cityInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                        DateTime.UtcNow.AddMinutes(10),
                        _localDataManager.GetCityStats());
                }
                catch
                {
                    setAsExpired = true;
                }

                if (setAsExpired || _cityInfoCache.Item2.Count == 0)
                {
                    _cityInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                    DateTime.UtcNow.AddMinutes(-5),
                    new List<CollatedStatsObject>());
                }

                _isLoadingCityInfoCache = false;
            }

            return _cityInfoCache.Item2;
        }

        public List<CollatedStatsObject> GetNeighborhoodInfoFromCache()
        {
            if (!_isLoadingNeighborhoodInfoCache && _neighborhoodInfoCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingNeighborhoodInfoCache = true;
                bool setAsExpired = false;

                try
                {
                    _neighborhoodInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                        DateTime.UtcNow.AddMinutes(10),
                        _localDataManager.GetNeighborhoodStats());
                }
                catch
                {
                    setAsExpired = true;
                }

                if (setAsExpired || _neighborhoodInfoCache.Item2.Count == 0)
                {
                    _neighborhoodInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                    DateTime.UtcNow.AddMinutes(-5),
                    new List<CollatedStatsObject>());
                }

                _isLoadingNeighborhoodInfoCache = false;
            }

            return _neighborhoodInfoCache.Item2;
        }

        public List<CollatedStatsObject> GetStreetInfoFromCache()
        {
            if (!_isLoadingStreetInfoCache && _streetInfoCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingStreetInfoCache = true;
                bool setAsExpired = false;

                try
                {
                    _streetInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                       DateTime.UtcNow.AddMinutes(10),
                       _localDataManager.GetStreetStats());
                }
                catch
                {
                    setAsExpired = true;
                }

                if (setAsExpired || _streetInfoCache.Item2.Count == 0)
                {
                    _streetInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                    DateTime.UtcNow.AddMinutes(-5),
                    new List<CollatedStatsObject>());
                }

                _isLoadingStreetInfoCache = false;

            }

            return _streetInfoCache.Item2;
        }

        public List<CollatedStatsObject> GetCollectionInfoFromCache()
        {
            if (!_isLoadingCollectionInfoCache && _collectionInfoCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingCollectionInfoCache = true;
                bool setAsExpired = false;

                try
                {
                    _collectionInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                        DateTime.UtcNow.AddMinutes(10),
                        _localDataManager.GetCollectionStats());
                }
                catch
                {
                    setAsExpired = true;
                }

                if (setAsExpired || _collectionInfoCache.Item2.Count == 0)
                {
                    _collectionInfoCache = new Tuple<DateTime, List<CollatedStatsObject>>(
                    DateTime.UtcNow.AddMinutes(-5),
                    new List<CollatedStatsObject>());
                }

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

        public Dictionary<int, StructureMetadata> GetStructureMetadataFromCache()
        {
            if (!_isLoadingNFTMetadataCache && _structureMetadataCache.Item1 < DateTime.UtcNow)
            {
                ReloadNFTMetaDataCache();
            }

            return _structureMetadataCache.Item2;
        }

        public Dictionary<int, LandVehicleMetadata> GetLandVehicleMetadataFromCache()
        {
            if (!_isLoadingNFTMetadataCache && _landvehicleMetadataCache.Item1 < DateTime.UtcNow)
            {
                ReloadNFTMetaDataCache();
            }

            return _landvehicleMetadataCache.Item2;
        }

        public Dictionary<int, int> GetCurrentNFTCountsFromCache()
        {
            if (!_isLoadingNFTMetadataCache && _nftCountsCache.Item1 < DateTime.UtcNow)
            {
                ReloadNFTMetaDataCache();
            }

            return _nftCountsCache.Item2;
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

            _structureMetadataCache = new Tuple<DateTime, Dictionary<int, StructureMetadata>>(
                DateTime.UtcNow.AddHours(1),
                newMetada.Where(m => m.Category == Consts.METADATA_TYPE_STRUCTURE)
                    .ToDictionary(m => m.Id, m => HelperFunctions.DecodeMetadata<StructureMetadata>(m.Metadata)));

            _landvehicleMetadataCache = new Tuple<DateTime, Dictionary<int, LandVehicleMetadata>>(
                DateTime.UtcNow.AddHours(1),
                newMetada.Where(m => m.Category == Consts.METADATA_TYPE_LANDVEHICLE)
                    .ToDictionary(m => m.Id, m => HelperFunctions.DecodeMetadata<LandVehicleMetadata>(m.Metadata)));

            _nftCountsCache = new Tuple<DateTime, Dictionary<int, int>>(
                DateTime.UtcNow.AddHours(1),
                _localDataManager.GetCurrentNFTCounts());

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

        public async Task<bool> GetUplandMaintenanceStatusFromCache()
        {
            if (_uplandMaintenanceStatusCache.Item1 < DateTime.UtcNow)
            {
                _uplandMaintenanceStatusCache = new Tuple<DateTime, bool>(
                    DateTime.UtcNow.AddMinutes(5),
                    await _uplandApiManager.GetIsInMaintenance());
            }

            return _uplandMaintenanceStatusCache.Item2;
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

        public Dictionary<int, string> GetNeighborhoodsFromCache()
        {
            return _neighborhoods;
        }

        public Dictionary<int, City> GetCitiesFromCache()
        {
            return _cities;
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
