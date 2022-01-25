using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class WebProcessor : IWebProcessor
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IUplandApiManager _uplandApiManager;

        private readonly List<Tuple<int, HashSet<long>>> _collectionProperties;

        private Dictionary<int, Tuple<DateTime, List<CachedForSaleProperty>>> _cityForSaleListCache;
        private Tuple<DateTime, Dictionary<long, string>> _propertyStructureCache;

        private Dictionary<int, bool> _isLoadingCityForSaleListCache;
        private bool _isLoadingPropertyStructureCache;

        public WebProcessor(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;

            InitializeCache();

            _collectionProperties = new List<Tuple<int, HashSet<long>>>();
            List<Tuple<int, long>> collectionProperties = _localDataManager.GetCollectionPropertyTable();

            foreach (int collectionId in collectionProperties.GroupBy(c => c.Item1).Select(g => g.First().Item1))
            {
                _collectionProperties.Add(new Tuple<int, HashSet<long>>(collectionId, collectionProperties.Where(c => c.Item1 == collectionId).Select(c => c.Item2).ToHashSet()));
            }
        }

        public async Task<UserProfile> GetWebUIProfile(string uplandUsername)
        {
            UserProfile profile = await _uplandApiManager.GetUserProfile(uplandUsername);

            profile.Rank = HelperFunctions.TranslateUserLevel(int.Parse(profile.Rank));
            profile.EOSAccount = _localDataManager.GetEOSAccountByUplandUsername(uplandUsername);

            Dictionary<long, Property> userProperties = _localDataManager
                .GetProperties(profile.Properties.Select(p => p.PropertyId).ToList())
                .ToDictionary(p => p.Id, p => p);

            Dictionary<long, string> userBuildings = GetPropertyStructuresFromCache();

            Dictionary<int, string> neighborhoods = _localDataManager.GetNeighborhoods()
                .ToDictionary(n => n.Id, n => n.Name);

            RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);

            if (registeredUser != null)
            {
                profile.RegisteredUser = true;
                profile.RegisteredUserId = registeredUser.Id;
                profile.RunCount = registeredUser.RunCount;
                profile.MaxRuns = Consts.FreeRuns + Convert.ToInt32(Math.Floor((double)(registeredUser.SendUPX / Consts.UPXPricePerRun)));
                profile.UPXToSupporter = Consts.SendUpxSupporterThreshold - registeredUser.SendUPX;
                profile.UPXToNextRun = Consts.UPXPricePerRun - registeredUser.SendUPX % Consts.UPXPricePerRun;
                profile.Supporter = registeredUser.Paid;
            }
            else
            {
                profile.RegisteredUser = false;
            }

            foreach (UserProfileProperty property in profile.Properties)
            {
                property.Address = userProperties[property.PropertyId].Address;
                property.City = Consts.Cities[userProperties[property.PropertyId].CityId];
                if (userProperties[property.PropertyId].NeighborhoodId == null)
                {
                    property.Neighborhood = "Unknown";
                }
                else
                {
                    property.Neighborhood = neighborhoods[userProperties[property.PropertyId].NeighborhoodId.Value];
                }
                property.Size = userProperties[property.PropertyId].Size;
                property.Mint = userProperties[property.PropertyId].Mint;
                property.Status = userProperties[property.PropertyId].Status;
                if (!userBuildings.ContainsKey(property.PropertyId))
                {
                    property.Building = "";
                }
                else
                {
                    property.Building = userBuildings[property.PropertyId];
                }
            }

            return profile;
        }

        public List<CachedForSaleProperty> GetForSaleProps(WebForSaleFilters filters)
        {
            List<CachedForSaleProperty> cityForSaleProps = GetCityForSaleListFromCache(filters.CityId);

            // Apply Filters
            cityForSaleProps = cityForSaleProps.Where(c =>
                   ((filters.Address == null || filters.Address.Trim() == "")
                    || (filters.Address != null && filters.Address.Trim() != "" && c.Address.ToLower().Contains(filters.Address.ToLower())))
                && ((filters.Owner == null || filters.Owner.Trim() == "")
                    || (filters.Owner != null && filters.Owner.Trim() != "" && c.Owner.ToLower().Contains(filters.Owner.ToLower())))
                && (filters.NeighborhoodIds.Count == 0
                    || filters.NeighborhoodIds.Contains(c.NeighborhoodId))
                && (filters.CollectionIds.Count == 0
                    || filters.CollectionIds.Any(i => c.CollectionIds.Contains(i)))
                && (filters.Buildings.Count == 0
                    || filters.Buildings.Contains(c.Building)
                && ((filters.Currency == null || filters.Currency == "")
                    || c.Currency == filters.Currency))
                ).ToList();

            // Sort
            if (filters.Asc)
            {
                if (filters.OrderBy == "PRICE")
                {
                    cityForSaleProps = cityForSaleProps.OrderBy(p => p.SortValue).ToList();
                }
                else
                {
                    cityForSaleProps = cityForSaleProps.OrderBy(p => p.Markup).ToList();
                }
            }
            else
            {
                if (filters.OrderBy == "PRICE")
                {
                    cityForSaleProps = cityForSaleProps.OrderByDescending(p => p.SortValue).ToList();
                }
                else
                {
                    cityForSaleProps = cityForSaleProps.OrderByDescending(p => p.Markup).ToList();
                }
            }

            return cityForSaleProps.Skip(filters.PageSize * (filters.Page - 1)).Take(filters.PageSize).ToList();
        }
        
        private void InitializeCache()
        {
            _isLoadingPropertyStructureCache = false;
            _propertyStructureCache = new Tuple<DateTime, Dictionary<long, string>>(DateTime.UtcNow.AddDays(-1), new Dictionary<long, string>());

            _isLoadingCityForSaleListCache = new Dictionary<int, bool>();
            _cityForSaleListCache = new Dictionary<int, Tuple<DateTime, List<CachedForSaleProperty>>>();
            foreach (int cityId in Consts.NON_BULLSHIT_CITY_IDS)
            {
                _isLoadingCityForSaleListCache.Add(cityId, false);
                _cityForSaleListCache.Add(cityId, new Tuple<DateTime, List<CachedForSaleProperty>>(DateTime.UtcNow.AddDays(-1), new List<CachedForSaleProperty>()));
            }
        }

        private Dictionary<long, string> GetPropertyStructuresFromCache()
        {
            if (!_isLoadingPropertyStructureCache && _propertyStructureCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingPropertyStructureCache = true;
                _propertyStructureCache = new Tuple<DateTime, Dictionary<long, string>>(
                    DateTime.UtcNow.AddDays(1),
                    _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType));
                _isLoadingPropertyStructureCache = false;
            }

            return _propertyStructureCache.Item2;
        }

        private List<CachedForSaleProperty> GetCityForSaleListFromCache(int cityId)
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

            return _cityForSaleListCache[cityId].Item2;
        }

        private List<int> GetCollectionIdListForPropertyId(long propertyId)
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
    }
}