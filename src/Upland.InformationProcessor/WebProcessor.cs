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
        private readonly Dictionary<int, string> _neighborhoods;

        private Dictionary<int, Tuple<DateTime, List<CachedForSaleProperty>>> _cityForSaleListCache;
        private Dictionary<int, Tuple<DateTime, List<CachedUnmintedProperty>>> _cityUnmintedCache;
        private Tuple<DateTime, Dictionary<long, string>> _propertyStructureCache;
        private Dictionary<int, Tuple<DateTime, List<CachedSaleHistoryEntry>>> _citySaleHistoryCache;
        private Tuple<DateTime, List<CachedSaleHistoryEntry>> _swapSaleHistoryCache;

        private Dictionary<int, bool> _isLoadingCityForSaleListCache;
        private Dictionary<int, bool> _isLoadingCityUnmintedCache;
        private bool _isLoadingPropertyStructureCache;
        private Dictionary<int, bool> _isLoadingCitySaleHistoryCache;
        private bool _isLoadingSwapSaleHistoryCache;

        public WebProcessor(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;

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
                property.CollectionIds = GetCollectionIdListForPropertyId(property.PropertyId);
            }

            return profile;
        }

        public List<CachedForSaleProperty> GetForSaleProps(WebForSaleFilters filters, bool noPaging)
        {
            List<CachedForSaleProperty> cityForSaleProps = GetCityForSaleListFromCache(filters.CityId);

            // Apply Filters
            cityForSaleProps = cityForSaleProps.Where(c =>
                   (filters.Address == null 
                    || filters.Address.Trim() == ""
                    || (filters.Address != null && filters.Address.Trim() != "" && c.Address.ToLower().Contains(filters.Address.ToLower())))
                && (filters.Owner == null 
                    || filters.Owner.Trim() == ""
                    || (filters.Owner != null && filters.Owner.Trim() != "" && c.Owner.ToLower().Contains(filters.Owner.ToLower())))
                && (filters.NeighborhoodIds.Count == 0
                    || filters.NeighborhoodIds.Contains(c.NeighborhoodId))
                && (filters.CollectionIds.Count == 0
                    || filters.CollectionIds.Any(i => c.CollectionIds.Contains(i)))
                && (filters.Buildings.Count == 0
                    || filters.Buildings.Contains(c.Building))
                && (filters.Currency == null 
                    || filters.Currency == "Any"
                    || c.Currency == filters.Currency)
                ).ToList();

            // Sort
            if (filters.Asc)
            {
                if (filters.OrderBy == "Price")
                {
                    cityForSaleProps = cityForSaleProps.OrderBy(p => p.SortValue).ToList();
                }
                else if (filters.OrderBy == "Markup")
                {
                    cityForSaleProps = cityForSaleProps.OrderBy(p => p.Markup).ToList();
                }
                else if (filters.OrderBy == "Mint")
                {
                    cityForSaleProps = cityForSaleProps.OrderBy(p => p.Mint).ToList();
                }
                else
                {
                    cityForSaleProps = cityForSaleProps.OrderBy(p => p.Size).ToList();
                }
            }
            else
            {
                if (filters.OrderBy == "Price")
                {
                    cityForSaleProps = cityForSaleProps.OrderByDescending(p => p.SortValue).ToList();
                }
                else if (filters.OrderBy == "Markup")
                {
                    cityForSaleProps = cityForSaleProps.OrderByDescending(p => p.Markup).ToList();
                }
                else if (filters.OrderBy == "Mint")
                {
                    cityForSaleProps = cityForSaleProps.OrderByDescending(p => p.Mint).ToList();
                }
                else
                {
                    cityForSaleProps = cityForSaleProps.OrderByDescending(p => p.Size).ToList();
                }
            }

            if (noPaging)
            {
                return cityForSaleProps;
            }

            return cityForSaleProps.Skip(filters.PageSize * (filters.Page - 1)).Take(filters.PageSize).ToList();
        }

        public List<CachedUnmintedProperty> GetUnmintedProperties(WebForSaleFilters filters, bool noPaging)
        {
            List<CachedUnmintedProperty> cityUnmintedProps = GetCityUnmintedFromCache(filters.CityId);

            // Apply Filters
            cityUnmintedProps = cityUnmintedProps.Where(c =>
                   (filters.Address == null
                    || filters.Address.Trim() == ""
                    || (filters.Address != null && filters.Address.Trim() != "" && c.Address.ToLower().Contains(filters.Address.ToLower())))
                && (filters.NeighborhoodIds.Count == 0
                    || filters.NeighborhoodIds.Contains(c.NeighborhoodId))
                && (filters.CollectionIds.Count == 0
                    || filters.CollectionIds.Any(i => c.CollectionIds.Contains(i)))
                && (filters.FSA == null
                    || filters.FSA.Value == c.FSA)
                ).ToList();

            // Sort
            if (filters.Asc)
            {
                if (filters.OrderBy == "Mint")
                {
                    cityUnmintedProps = cityUnmintedProps.OrderBy(p => p.Mint).ToList();
                }
                else
                {
                    cityUnmintedProps = cityUnmintedProps.OrderBy(p => p.Size).ToList();
                }
            }
            else
            {
                if (filters.OrderBy == "Mint")
                {
                    cityUnmintedProps = cityUnmintedProps.OrderByDescending(p => p.Mint).ToList();
                }
                else
                {
                    cityUnmintedProps = cityUnmintedProps.OrderByDescending(p => p.Size).ToList();
                }
            }

            if (noPaging)
            {
                return cityUnmintedProps;
            }

            return cityUnmintedProps.Skip(filters.PageSize * (filters.Page - 1)).Take(filters.PageSize).ToList();
        }

        public List<CachedSaleHistoryEntry> GetSaleHistoryEntries(WebSaleHistoryFilters filters, bool noPaging)
        {
            List<CachedSaleHistoryEntry> saleHistoryEntries = new List<CachedSaleHistoryEntry>();

            if (filters.SearchByType == "City")
            {
                if (filters.EntryType.Count == 0 || filters.EntryType.Contains("Swap"))
                {
                    saleHistoryEntries.AddRange(GetSwapSaleHistoryFromCache().Where(e => e.Property.CityId == filters.SearchByCityId || e.OfferProperty.CityId == filters.SearchByCityId));
                }

                saleHistoryEntries.AddRange(GetCitySaleHistoryFromCache(filters.SearchByCityId)
                    .Where(e => filters.EntryType.Count == 0 
                        || (e.Offer && e.OfferProperty == null && filters.EntryType.Contains("Offer"))
                        || (!e.Offer && filters.EntryType.Contains("Sale"))
                        || (e.Offer && e.OfferProperty != null && filters.EntryType.Contains("Swap"))
                    ));
            }
            else if (filters.SearchByType == "Username")
            {
                if (filters.EntryType.Count == 0 || filters.EntryType.Contains("Swap"))
                {
                    saleHistoryEntries.AddRange(GetSwapSaleHistoryFromCache().Where(e => e.Seller == filters.SearchByUsername || e.Buyer == filters.SearchByUsername));
                }

                foreach (KeyValuePair<int, Tuple<DateTime, List<CachedSaleHistoryEntry>>> cacheEntry in _citySaleHistoryCache)
                {
                    saleHistoryEntries.AddRange(GetCitySaleHistoryFromCache(cacheEntry.Key)
                        .Where(e => (e.Seller == filters.SearchByUsername || e.Buyer == filters.SearchByUsername) &&
                            (filters.EntryType.Count == 0
                            || (e.Offer && e.OfferProperty == null && filters.EntryType.Contains("Offer"))
                            || (!e.Offer && filters.EntryType.Contains("Sale"))
                            || (e.Offer && e.OfferProperty != null && filters.EntryType.Contains("Swap")))
                    ));
                }
            }

            // Apply Filters
            saleHistoryEntries = saleHistoryEntries.Where(c =>
                   (filters.Address == null
                    || filters.Address.Trim() == ""
                    || (filters.Address != null && filters.Address.Trim() != "" && ( 
                        c.Property.Address.ToLower().Contains(filters.Address.ToLower())
                        || (c.OfferProperty != null && c.OfferProperty.Address.ToLower().Contains(filters.Address.ToLower())))))
                && (filters.NeighborhoodIds.Count == 0
                    || (filters.NeighborhoodIds.Contains(c.Property.NeighborhoodId)
                        || (c.OfferProperty != null && filters.NeighborhoodIds.Contains(c.OfferProperty.NeighborhoodId))))
                && (filters.CollectionIds.Count == 0
                    || (filters.CollectionIds.Any(i => c.Property.CollectionIds.Contains(i))
                        || (c.OfferProperty != null && filters.CollectionIds.Any(i => c.OfferProperty.CollectionIds.Contains(i)))))
                && (filters.Currency == null
                    || filters.Currency == "Any"
                    || c.Currency == filters.Currency)
                ).ToList();

            // Sort
            if (filters.Asc)
            {
                if (filters.OrderBy == "Seller")
                {
                    saleHistoryEntries = saleHistoryEntries.OrderBy(p => p.Seller).ToList();
                }
                else if(filters.OrderBy == "Buyer")
                {
                    saleHistoryEntries = saleHistoryEntries.OrderBy(p => p.Buyer).ToList();
                }
                else if (filters.OrderBy == "Price")
                {
                    saleHistoryEntries = saleHistoryEntries.OrderBy(p => p.Price).ToList();
                }
                else
                {
                    saleHistoryEntries = saleHistoryEntries.OrderBy(p => p.TransactionDateTime).ToList();
                }
            }
            else
            {
                if (filters.OrderBy == "Seller")
                {
                    saleHistoryEntries = saleHistoryEntries.OrderByDescending(p => p.Seller).ToList();
                }
                else if (filters.OrderBy == "Buyer")
                {
                    saleHistoryEntries = saleHistoryEntries.OrderByDescending(p => p.Buyer).ToList();
                }
                else if (filters.OrderBy == "Price")
                {
                    saleHistoryEntries = saleHistoryEntries.OrderByDescending(p => p.Price).ToList();
                }
                else
                {
                    saleHistoryEntries = saleHistoryEntries.OrderByDescending(p => p.TransactionDateTime).ToList();
                }
            }

            if (noPaging)
            {
                return saleHistoryEntries;
            }

            return saleHistoryEntries.Skip(filters.PageSize * (filters.Page - 1)).Take(filters.PageSize).ToList();
        }

        public List<string> ConvertListCachedForSalePropertyToCSV(List<CachedForSaleProperty> cachedForSaleProperties)
        {
            List<string> csvString = new List<string>();

            csvString.Add("City,Address,Neighborhood,Size,Mint,Price,Currency,Markup,Owner,CollectionIds,Building");

            foreach (CachedForSaleProperty prop in cachedForSaleProperties)
            {
                string propString = "";
                propString += Consts.Cities[prop.CityId] + ",";
                propString += prop.Address.Replace(",", " ");
                propString += _neighborhoods[prop.NeighborhoodId].Replace(",", "") + ",";
                propString += prop.Size + ",";
                propString += prop.Mint + ",";
                propString += prop.Price + ",";
                propString += prop.Currency + ",";
                propString += prop.Markup + ",";
                propString += prop.Owner + ",";
                propString += string.Join(" ", prop.CollectionIds) + ",";
                propString += prop.Building + ",";

                csvString.Add(propString);
            }

            return csvString;
        }

        public List<string> ConvertListCachedUnmintedPropertyToCSV(List<CachedUnmintedProperty> cachedUnmintedProperties)
        {
            List<string> csvString = new List<string>();

            csvString.Add("City,Address,Neighborhood,Size,Mint,FSA,CollectionIds");

            foreach (CachedUnmintedProperty prop in cachedUnmintedProperties)
            {
                string propString = "";
                propString += Consts.Cities[prop.CityId] + ",";
                propString += prop.Address.Replace(",", " ") + ",";
                propString += _neighborhoods[prop.NeighborhoodId].Replace(",", "") + ",";
                propString += prop.Size + ",";
                propString += prop.Mint + ",";
                propString += prop.FSA + ",";
                propString += string.Join(" ", prop.CollectionIds) + ",";

                csvString.Add(propString);
            }

            return csvString;
        }

        public List<string> ConvertListCachedSaleHistoryEntriesToCSV(List<CachedSaleHistoryEntry> cachedSaleHistoryEntries)
        {
            List<string> csvString = new List<string>();

            csvString.Add("TransactionDateTime,Seller,Buyer,Price,Currency,Offer,City,Address,Neighborhood,Mint,CollectionIds,OfferPropCity,OfferPropAddress,OfferPropNeighborhood,OfferPropMint,OfferPropCollectionIds");

            foreach (CachedSaleHistoryEntry entry in cachedSaleHistoryEntries)
            {
                string entryString = "";
                entryString += entry.TransactionDateTime.ToString("MM-dd-yyyy HH:mm:ss") + ",";
                entryString += entry.Seller + ",";
                entryString += entry.Buyer + ",";
                entryString += entry.Price == null ? "," : entry.Price + ",";
                entryString += entry.Currency + ",";
                entryString += entry.Offer ? "True," : "False,";

                entryString += Consts.Cities[entry.Property.CityId] + ",";
                entryString += entry.Property.Address.Replace(",", " ") + ",";
                entryString += _neighborhoods[entry.Property.NeighborhoodId].Replace(",", "") + ",";
                entryString += entry.Property.Mint + ",";
                entryString += string.Join(" ", entry.Property.CollectionIds) + ",";

                if (entry.OfferProperty == null)
                {
                    entryString += ",,,,,";
                }
                else
                {
                    entryString += Consts.Cities[entry.OfferProperty.CityId] + ",";
                    entryString += entry.OfferProperty.Address.Replace(",", " ") + ",";
                    entryString += _neighborhoods[entry.OfferProperty.NeighborhoodId].Replace(",", "") + ",";
                    entryString += entry.OfferProperty.Mint + ",";
                    entryString += string.Join(" ", entry.OfferProperty.CollectionIds) + ",";
                }

                csvString.Add(entryString);
            }

            return csvString;
        }

        #region Caching

        private void InitializeCache()
        {
            _isLoadingPropertyStructureCache = false;
            _propertyStructureCache = new Tuple<DateTime, Dictionary<long, string>>(DateTime.UtcNow.AddDays(-1), new Dictionary<long, string>());

            _isLoadingCityForSaleListCache = new Dictionary<int, bool>();
            _isLoadingCityUnmintedCache = new Dictionary<int, bool>();
            _cityForSaleListCache = new Dictionary<int, Tuple<DateTime, List<CachedForSaleProperty>>>();
            _cityUnmintedCache = new Dictionary<int, Tuple<DateTime, List<CachedUnmintedProperty>>>();

            _isLoadingSwapSaleHistoryCache = false;
            _swapSaleHistoryCache = new Tuple<DateTime, List<CachedSaleHistoryEntry>>(DateTime.UtcNow.AddDays(-1), new List<CachedSaleHistoryEntry>());
            _isLoadingCitySaleHistoryCache = new Dictionary<int, bool>();
            _citySaleHistoryCache = new Dictionary<int, Tuple<DateTime, List<CachedSaleHistoryEntry>>>();

            foreach (int cityId in Consts.NON_BULLSHIT_CITY_IDS)
            {
                _isLoadingCityForSaleListCache.Add(cityId, false);
                _isLoadingCityUnmintedCache.Add(cityId, false);
                _cityForSaleListCache.Add(cityId, new Tuple<DateTime, List<CachedForSaleProperty>>(DateTime.UtcNow.AddDays(-1), new List<CachedForSaleProperty>()));
                _cityUnmintedCache.Add(cityId, new Tuple<DateTime, List<CachedUnmintedProperty>>(DateTime.UtcNow.AddDays(-1), new List<CachedUnmintedProperty>()));

                _isLoadingCitySaleHistoryCache.Add(cityId, false);
                _citySaleHistoryCache.Add(cityId, new Tuple<DateTime, List<CachedSaleHistoryEntry>>(DateTime.UtcNow.AddDays(-1), new List<CachedSaleHistoryEntry>()));
            }
        }

        private Dictionary<long, string> GetPropertyStructuresFromCache()
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

            RemoveExpiredSalesEntries();

            return _cityForSaleListCache[cityId].Item2;
        }

        private List<CachedUnmintedProperty> GetCityUnmintedFromCache(int cityId)
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

        private List<CachedSaleHistoryEntry> GetCitySaleHistoryFromCache(int cityId)
        {
            if (!_isLoadingCitySaleHistoryCache[cityId] && _citySaleHistoryCache[cityId].Item1 < DateTime.UtcNow)
            {
                _isLoadingCitySaleHistoryCache[cityId] = true;
                _citySaleHistoryCache[cityId] = new Tuple<DateTime, List<CachedSaleHistoryEntry>>(
                    DateTime.UtcNow.AddMinutes(5),
                   _localDataManager.GetCachedSaleHistoryEntriesByCityId(cityId));

                foreach (CachedSaleHistoryEntry entry in _citySaleHistoryCache[cityId].Item2)
                {
                    entry.Property.CollectionIds = GetCollectionIdListForPropertyId(entry.Property.Id);
                }

                _isLoadingCitySaleHistoryCache[cityId] = false;
            }

            RemoveExpiredSaleHistoryEntries();

            return _citySaleHistoryCache[cityId].Item2;
        }

        private List<CachedSaleHistoryEntry> GetSwapSaleHistoryFromCache()
        {
            if (!_isLoadingSwapSaleHistoryCache && _swapSaleHistoryCache.Item1 < DateTime.UtcNow)
            {
                _isLoadingSwapSaleHistoryCache = true;
                _swapSaleHistoryCache = new Tuple<DateTime, List<CachedSaleHistoryEntry>>(
                    DateTime.UtcNow.AddMinutes(5),
                    _localDataManager.GetCachedSaleHistorySwapEntries());

                foreach (CachedSaleHistoryEntry entry in _swapSaleHistoryCache.Item2)
                {
                    entry.Property.CollectionIds = GetCollectionIdListForPropertyId(entry.Property.Id);
                    entry.OfferProperty.CollectionIds = GetCollectionIdListForPropertyId(entry.OfferProperty.Id);
                }

                _isLoadingSwapSaleHistoryCache = false;
            }

            RemoveExpiredSaleHistoryEntries();

            return _swapSaleHistoryCache.Item2;
        }

        private void RemoveExpiredSalesEntries()
        {
            foreach (KeyValuePair<int, Tuple<DateTime, List<CachedForSaleProperty>>> cacheEntry in _cityForSaleListCache)
            {
                // Make sure its not loading, and the entry is expired, and that there is actually something to expire
                if (!_isLoadingCityForSaleListCache[cacheEntry.Key] && cacheEntry.Value.Item1 < DateTime.UtcNow && _cityForSaleListCache[cacheEntry.Key].Item2.Count > 0)
                {
                    _isLoadingCityForSaleListCache[cacheEntry.Key] = true;

                    _cityForSaleListCache[cacheEntry.Key] = new Tuple<DateTime, List<CachedForSaleProperty>>(
                        DateTime.UtcNow.AddDays(-1),
                       new List<CachedForSaleProperty>());

                    _isLoadingCityForSaleListCache[cacheEntry.Key] = false;
                }

            }
        }

        private void RemoveExpiredUnmintedEntries()
        {
            foreach (KeyValuePair<int, Tuple<DateTime, List<CachedUnmintedProperty>>> cacheEntry in _cityUnmintedCache)
            {
                // Make sure its not loading, and the entry is expired, and that there is actually something to expire
                if (!_isLoadingCityUnmintedCache[cacheEntry.Key] && cacheEntry.Value.Item1 < DateTime.UtcNow && _cityUnmintedCache[cacheEntry.Key].Item2.Count > 0)
                {
                    _isLoadingCityUnmintedCache[cacheEntry.Key] = true;

                    _cityUnmintedCache[cacheEntry.Key] = new Tuple<DateTime, List<CachedUnmintedProperty>>(
                        DateTime.UtcNow.AddDays(-1),
                       new List<CachedUnmintedProperty>());

                    _isLoadingCityUnmintedCache[cacheEntry.Key] = false;
                }
            }
        }

        private void RemoveExpiredSaleHistoryEntries()
        {
            foreach (KeyValuePair<int, Tuple<DateTime, List<CachedSaleHistoryEntry>>> cacheEntry in _citySaleHistoryCache)
            {
                // Make sure its not loading, and the entry is expired, and that there is actually something to expire
                if (!_isLoadingCityForSaleListCache[cacheEntry.Key] && cacheEntry.Value.Item1 < DateTime.UtcNow && _citySaleHistoryCache[cacheEntry.Key].Item2.Count > 0)
                {
                    _isLoadingCityForSaleListCache[cacheEntry.Key] = true;

                    _citySaleHistoryCache[cacheEntry.Key] = new Tuple<DateTime, List<CachedSaleHistoryEntry>>(
                        DateTime.UtcNow.AddDays(-1),
                       new List<CachedSaleHistoryEntry>());

                    _isLoadingCityForSaleListCache[cacheEntry.Key] = false;
                }
            }

            if (!_isLoadingSwapSaleHistoryCache && _swapSaleHistoryCache.Item1 < DateTime.UtcNow && _swapSaleHistoryCache.Item2.Count > 0)
            {
                _isLoadingSwapSaleHistoryCache = true;

                _swapSaleHistoryCache = new Tuple<DateTime, List<CachedSaleHistoryEntry>>(
                    DateTime.UtcNow.AddDays(-1),
                   new List<CachedSaleHistoryEntry>());

                _isLoadingSwapSaleHistoryCache = false;
            }
        }

        #endregion Caching

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