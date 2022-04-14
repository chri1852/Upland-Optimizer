using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Enums;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class WebProcessor : IWebProcessor
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IUplandApiManager _uplandApiManager;
        private readonly ICachingProcessor _cachingProcessor;

        public WebProcessor(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager, ICachingProcessor cachingProcessor)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;
            _cachingProcessor = cachingProcessor;
        }

        public bool GetIsBlockchainUpdatesDisabled()
        {
            return _cachingProcessor.GetIsBlockchainUpdatesDisabledFromCache();
        }

        public string GetLatestAnnouncement()
        {
            return _cachingProcessor.GetLatestAnnouncemenFromCache();
        }

        public async Task<UserProfile> GetWebUIProfile(string uplandUsername)
        {
            UserProfile profile = await _uplandApiManager.GetUserProfile(uplandUsername);
            Dictionary<long, AcquiredInfo> propertyAcquistionInfo = _localDataManager.GetAcquiredOnByPlayer(uplandUsername).ToDictionary(a => a.PropertyId, a => a);

            profile.Rank = HelperFunctions.TranslateUserLevel(int.Parse(profile.Rank));
            profile.EOSAccount = _localDataManager.GetEOSAccountByUplandUsername(uplandUsername).EOSAccount;

            Dictionary<long, Property> userProperties = _localDataManager
                .GetProperties(profile.Properties.Select(p => p.PropertyId).ToList())
                .ToDictionary(p => p.Id, p => p);

            Dictionary<long, string> userBuildings = _cachingProcessor.GetPropertyStructuresFromCache();

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
                property.CollectionIds = _cachingProcessor.GetCollectionIdListForPropertyId(property.PropertyId);

                if (propertyAcquistionInfo.ContainsKey(property.PropertyId))
                {
                    property.Minted = propertyAcquistionInfo[property.PropertyId].Minted;
                    property.AcquiredOn = propertyAcquistionInfo[property.PropertyId].AcquiredDateTime;
                }
            }

            // Now Lets get the Profile's NFTs
            List<NFT> ownerNFTs = _localDataManager.GetNFTsByOwnerEOS(profile.EOSAccount);
            profile.ProfileNFTs = new List<WebNFT>();
            WebNFTFilters blankFilters = new WebNFTFilters();
            blankFilters.Filters = new WebNFT();
            blankFilters.SortBy = null;
            blankFilters.SortDescending = false;
            blankFilters.Filters.Owner = "";
            blankFilters.Filters.IsVariantFilter = 0;
            blankFilters.Filters.HomeTeam = "";
            blankFilters.Filters.Opponent = "";

            profile.ProfileNFTs.AddRange(FilterAndSortBlockExplorerNFTs(ownerNFTs, blankFilters));
            profile.ProfileNFTs.AddRange(FilterAndSortStructureOrnamentNFTs(ownerNFTs, blankFilters));
            profile.ProfileNFTs.AddRange(FilterAndSortSpiritHlwnNFTs(ownerNFTs, blankFilters));
            profile.ProfileNFTs.AddRange(FilterAndSortEssentialNFTs(ownerNFTs, blankFilters));
            profile.ProfileNFTs.AddRange(FilterAndSortMementosNFTs(ownerNFTs, blankFilters));
            profile.ProfileNFTs.AddRange(FilterAndSortStructureNFTs(ownerNFTs, blankFilters));

            return profile;
        }

        public List<CachedForSaleProperty> GetForSaleProps(WebForSaleFilters filters, bool noPaging)
        {
            List<CachedForSaleProperty> cityForSaleProps = _cachingProcessor.GetCityForSaleListFromCache(filters.CityId);

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
                && (filters.Currency == null
                    || filters.Currency == "Any"
                    || c.Currency == filters.Currency)
                ).ToList();

            Dictionary<long, string> userBuildings = _cachingProcessor.GetPropertyStructuresFromCache();

            foreach (CachedForSaleProperty prop in cityForSaleProps)
            {
                if (userBuildings.ContainsKey(prop.Id))
                {
                    prop.Building = userBuildings[prop.Id];
                }
                else
                {
                    prop.Building = "";
                }
            }

            if (filters.Buildings.Count > 0)
            {
                cityForSaleProps = cityForSaleProps.Where(c => filters.Buildings.Contains(c.Building)).ToList();
            }

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
            List<CachedUnmintedProperty> cityUnmintedProps = _cachingProcessor.GetCityUnmintedFromCache(filters.CityId);

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
            // Smooth the From and To Date
            filters.FromDate = new DateTime(filters.FromDate.Year, filters.FromDate.Month, filters.FromDate.Day, 0, 0, 0);
            filters.ToDate = new DateTime(filters.ToDate.Year, filters.ToDate.Month, filters.ToDate.Day, 23, 59, 59);

            List <CachedSaleHistoryEntry> saleHistoryEntries = _localDataManager.GetCachedSaleHistoryEntries(filters)
                .Where(c => (filters.NeighborhoodIds.Count == 0
                    || filters.NeighborhoodIds.Contains(c.Property.NeighborhoodId)
                    || (c.OfferProperty != null && filters.NeighborhoodIds.Contains(c.OfferProperty.NeighborhoodId)))).ToList();

            // Gather the collections
            foreach (CachedSaleHistoryEntry entry in saleHistoryEntries)
            {
                entry.Property.CollectionIds = _cachingProcessor.GetCollectionIdListForPropertyId(entry.Property.Id);
                if (entry.OfferProperty != null)
                {
                    entry.OfferProperty.CollectionIds = _cachingProcessor.GetCollectionIdListForPropertyId(entry.OfferProperty.Id);
                }
            }

            // Apply Filters
            saleHistoryEntries = saleHistoryEntries
                .Where(c => filters.CollectionIds.Count == 0
                    || filters.CollectionIds.Any(i => c.Property.CollectionIds.Contains(i))
                    || (c.OfferProperty != null && filters.CollectionIds.Any(i => c.OfferProperty.CollectionIds.Contains(i)))
                ).ToList();

            // Sort
            saleHistoryEntries = saleHistoryEntries.OrderByDescending(p => p.TransactionDateTime).ToList();

            if (noPaging)
            {
                return saleHistoryEntries;
            }

            // return the first 25k lines for fast paging once loaded
            saleHistoryEntries = saleHistoryEntries.Take(25000).ToList();

            return saleHistoryEntries;
        }

        public List<CollatedStatsObject> GetInfoByType(StatsTypes statsType)
        {
            switch (statsType)
            {
                case StatsTypes.City:
                    return _cachingProcessor.GetCityInfoFromCache();
                case StatsTypes.Neighborhood:
                    return _cachingProcessor.GetNeighborhoodInfoFromCache();
                case StatsTypes.Street:
                    return _cachingProcessor.GetStreetInfoFromCache();
                case StatsTypes.Collection:
                    return _cachingProcessor.GetCollectionInfoFromCache();
                default:
                    return _cachingProcessor.GetCityInfoFromCache();
            }
        }

        public List<UIPropertyHistory> GetPropertyHistory(long propertyId)
        {
            List<UIPropertyHistory> history = new List<UIPropertyHistory>();

            Property property = _localDataManager.GetProperty(propertyId);
            if (property != null)
            {
                if (property.MintedBy != null && property.MintedOn != null)
                {
                    EOSUser minter = _localDataManager.GetUplandUsernameByEOSAccount(property.MintedBy);

                    history.Add(new UIPropertyHistory
                    {
                        DateTime = property.MintedOn.Value,
                        Price = string.Format("{0:N2} UPX", property.Mint),
                        Action = "Minted",
                        NewOwner = minter.UplandUsername
                    });
                }

                List<CachedSaleHistoryEntry> propRawHistory = _localDataManager.GetCachedSaleHistoryEntriesByPropertyId(propertyId);

                foreach (CachedSaleHistoryEntry entry in propRawHistory)
                {
                    if (entry.Offer && entry.OfferProperty == null)
                    {
                        history.Add(new UIPropertyHistory
                        {
                            DateTime = entry.TransactionDateTime,
                            Price = entry.Currency == "USD" ? string.Format("{0:N2} USD", entry.Price) : string.Format("{0:N2} UPX", entry.Price),
                            Action = "Accepted Offer",
                            NewOwner = entry.Buyer
                        });
                    }
                    else if (entry.Offer && entry.OfferProperty != null)
                    {
                        if (entry.Property.Id == propertyId)
                        {
                            history.Add(new UIPropertyHistory
                            {
                                DateTime = entry.TransactionDateTime,
                                Price = string.Format("{0}, {1}", entry.OfferProperty.Address, Consts.Cities[entry.OfferProperty.CityId]),
                                Action = "Swap",
                                NewOwner = entry.Buyer
                            });
                        }
                        else
                        {
                            history.Add(new UIPropertyHistory
                            {
                                DateTime = entry.TransactionDateTime,
                                Price = string.Format("{0}, {1}", entry.Property.Address, Consts.Cities[entry.Property.CityId]),
                                Action = "Swap",
                                NewOwner = entry.Seller
                            });
                        }    
                    }
                    else if (!entry.Offer)
                    {
                        history.Add(new UIPropertyHistory
                        {
                            DateTime = entry.TransactionDateTime,
                            Price = entry.Currency == "USD" ? string.Format("{0:N2} USD", entry.Price) : string.Format("{0:N2} UPX", entry.Price),
                            Action = "Bought",
                            NewOwner = entry.Buyer
                        });
                    }
                }
            }

            return history.OrderByDescending(h => h.DateTime).ToList();
        }

        public List<WebNFT> SearchNFTs(WebNFTFilters filters)
        {
            List<int> matchingMetadata = new List<int>();

            switch (filters.Category)
            {
                case Consts.METADATA_TYPE_BLKEXPLORER:
                    matchingMetadata = _cachingProcessor.GetBlockExplorerMetadataFromCache()
                        .Where(m =>
                            (string.IsNullOrWhiteSpace(filters.Filters.Name) || m.Value.DisplayName.IndexOf(filters.Filters.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.SeriesName) || m.Value.SeriesName.IndexOf(filters.Filters.SeriesName, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Rarity) || m.Value.RarityLevel.IndexOf(filters.Filters.Rarity, StringComparison.OrdinalIgnoreCase) >= 0))
                        .Select(m => m.Key).ToList();
                    break;
                case Consts.METADATA_TYPE_ESSENTIAL:
                    matchingMetadata = _cachingProcessor.GetEssentialMetadataFromCache()
                        .Where(m =>
                            (string.IsNullOrWhiteSpace(filters.Filters.Name) || m.Value.PlayerFullName.IndexOf(filters.Filters.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Team) || m.Value.TeamName.IndexOf(filters.Filters.Team, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Position) || m.Value.PlayerPosition.IndexOf(filters.Filters.Position, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Year) || m.Value.Season == int.Parse(filters.Filters.Year))
                            && (string.IsNullOrWhiteSpace(filters.Filters.ModelType) || m.Value.ModelType.IndexOf(filters.Filters.ModelType, StringComparison.OrdinalIgnoreCase) >= 0))
                        .Select(m => m.Key).ToList();
                    break;
                case Consts.METADATA_TYPE_MEMENTO:
                    matchingMetadata = _cachingProcessor.GetMementoMetadataFromCache()
                        .Where(m =>
                            (string.IsNullOrWhiteSpace(filters.Filters.Name) || m.Value.PlayerFullName.IndexOf(filters.Filters.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Team) || m.Value.TeamName.IndexOf(filters.Filters.Team, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Position) || m.Value.PlayerPosition.IndexOf(filters.Filters.Position, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Year) || m.Value.Season == int.Parse(filters.Filters.Year))
                            && (string.IsNullOrWhiteSpace(filters.Filters.ModelType) || m.Value.ModelType.IndexOf(filters.Filters.ModelType, StringComparison.OrdinalIgnoreCase) >= 0))
                        .Select(m => m.Key).ToList();
                    break;
                case Consts.METADATA_TYPE_SPIRITHLWN:
                    matchingMetadata = _cachingProcessor.GetSpiritHlwnMetadataFromCache()
                        .Where(m =>
                            (string.IsNullOrWhiteSpace(filters.Filters.Name) || m.Value.DisplayName.IndexOf(filters.Filters.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Rarity) || m.Value.RarityLevel.IndexOf(filters.Filters.Rarity, StringComparison.OrdinalIgnoreCase) >= 0))
                        .Select(m => m.Key).ToList();
                    break;
                case Consts.METADATA_TYPE_STRUCTORNMT:
                    matchingMetadata = _cachingProcessor.GetStructornmtMetadataFromCache()
                        .Where(m =>
                            (string.IsNullOrWhiteSpace(filters.Filters.Name) || m.Value.DisplayName.IndexOf(filters.Filters.Name, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.BuildingType) || m.Value.BuildingType.IndexOf(filters.Filters.BuildingType, StringComparison.OrdinalIgnoreCase) >= 0)
                            && (string.IsNullOrWhiteSpace(filters.Filters.Rarity) || m.Value.RarityLevel.IndexOf(filters.Filters.Rarity, StringComparison.OrdinalIgnoreCase) >= 0))
                        .Select(m => m.Key).ToList();
                    break;
                default:
                    matchingMetadata = new List<int>();
                    break;
            }

            List<NFT> matchingNfts = _localDataManager.GetNFTsByNFTMetadataId(matchingMetadata).Where(n => !n.Burned || filters.IncludeBurned).ToList();
            List<WebNFT> webNfts = new List<WebNFT>();

            // Build the WebNFTs
            switch (filters.Category)
            {
                case Consts.METADATA_TYPE_BLKEXPLORER:
                    webNfts = FilterAndSortBlockExplorerNFTs(matchingNfts, filters);
                    break;
                case Consts.METADATA_TYPE_ESSENTIAL:
                    webNfts = FilterAndSortEssentialNFTs(matchingNfts, filters);
                    break;
                case Consts.METADATA_TYPE_MEMENTO:
                    webNfts = FilterAndSortMementosNFTs(matchingNfts, filters);
                    break;
                case Consts.METADATA_TYPE_SPIRITHLWN:
                    webNfts = FilterAndSortSpiritHlwnNFTs(matchingNfts, filters);
                    break;
                case Consts.METADATA_TYPE_STRUCTORNMT:
                    webNfts = FilterAndSortStructureOrnamentNFTs(matchingNfts, filters);
                    break;
                default:
                    webNfts = new List<WebNFT>();
                    break;
            }

            if (filters.NoPaging)
            {
                return webNfts;
            }

            return webNfts.Take(10000).ToList();
        }

        public List<WebNFTHistory> GetNFTHistory(int dGoodId)
        {
            List<NFTHistory> nftHistory = _localDataManager.GetNftHistoryByDGoodId(dGoodId).OrderBy(n => n.ObtainedOn).ToList();

            List<WebNFTHistory> webHistory = new List<WebNFTHistory>();

            for (int i = 0; i < nftHistory.Count; i++)
            {
                webHistory.Add(new WebNFTHistory
                {
                    DateTime = nftHistory[i].ObtainedOn,
                    Event = string.Format("Obtained By {0}", _localDataManager.GetUplandUsernameByEOSAccount(nftHistory[i].Owner).UplandUsername)
                });

                if (nftHistory[i].DisposedOn != null)
                {
                    webHistory.Add(new WebNFTHistory
                    {
                        DateTime = nftHistory[i].DisposedOn.Value,
                        Event = string.Format("Disposed By {0}", _localDataManager.GetUplandUsernameByEOSAccount(nftHistory[i].Owner).UplandUsername)
                    });
                }
            }

            return webHistory.OrderByDescending(h => h.DateTime).ToList();
        }

        public List<string> ConvertListCachedForSalePropertyToCSV(List<CachedForSaleProperty> cachedForSaleProperties)
        {
            Dictionary<int, string> neighborhoods = _cachingProcessor.GetNeighborhoodsFromCache();

            List<string> csvString = new List<string>();

            csvString.Add("City,Address,Neighborhood,Size,Mint,Price,Currency,Markup,Owner,CollectionIds,Building");

            foreach (CachedForSaleProperty prop in cachedForSaleProperties)
            {
                string propString = "";
                propString += Consts.Cities[prop.CityId] + ",";
                propString += prop.Address.Replace(",", " ") + ","; ;
                propString += neighborhoods[prop.NeighborhoodId].Replace(",", "") + ",";
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
            Dictionary<int, string> neighborhoods = _cachingProcessor.GetNeighborhoodsFromCache();

            List<string> csvString = new List<string>();

            csvString.Add("City,Address,Neighborhood,Size,Mint,FSA,CollectionIds");

            foreach (CachedUnmintedProperty prop in cachedUnmintedProperties)
            {
                string propString = "";
                propString += Consts.Cities[prop.CityId] + ",";
                propString += prop.Address.Replace(",", " ") + ",";
                propString += neighborhoods[prop.NeighborhoodId].Replace(",", "") + ",";
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

            Dictionary<int, string> neighborhoods = _cachingProcessor.GetNeighborhoodsFromCache();

            csvString.Add("TransactionDateTime,Seller,Buyer,Price,Markup,Currency,Offer,City,Address,Neighborhood,Mint,CollectionIds,OfferPropCity,OfferPropAddress,OfferPropNeighborhood,OfferPropMint,OfferPropCollectionIds");

            foreach (CachedSaleHistoryEntry entry in cachedSaleHistoryEntries)
            {
                string entryString = "";
                entryString += entry.TransactionDateTime.ToString("MM-dd-yyyy HH:mm:ss") + ",";
                entryString += entry.Seller + ",";
                entryString += entry.Buyer + ",";
                entryString += entry.Price == null ? "," : entry.Price + ",";
                entryString += entry.Markup == null ? "," : entry.Markup + ",";
                entryString += entry.Currency + ",";
                entryString += entry.Offer ? "True," : "False,";

                entryString += Consts.Cities[entry.Property.CityId] + ",";
                entryString += entry.Property.Address.Replace(",", " ") + ",";
                entryString += neighborhoods.ContainsKey(entry.Property.NeighborhoodId) ? neighborhoods[entry.Property.NeighborhoodId].Replace(",", "") + "," : ",";
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
                    entryString += neighborhoods.ContainsKey(entry.OfferProperty.NeighborhoodId) ? neighborhoods[entry.OfferProperty.NeighborhoodId].Replace(",", "") + "," : ",";
                    entryString += entry.OfferProperty.Mint + ",";
                    entryString += string.Join(" ", entry.OfferProperty.CollectionIds) + ",";
                }

                csvString.Add(entryString);
            }

            return csvString;
        }

        public List<string> ConvertListWebNFTSToCSV(List<WebNFT> nfts, string category)
        {
            List<string> csvString = new List<string>();

            string headerString = "DGoodId,SerialNumber,CurrentSupply,MaxSupply,Name,Owner";
            switch (category)
            {
                case Consts.METADATA_TYPE_BLKEXPLORER:
                    headerString += ",SeriesName,Rarity";
                    break;
                case Consts.METADATA_TYPE_ESSENTIAL:
                    headerString += ",Team,Year,Position,FanPoints,ModelType,IsVariant";
                    break;
                case Consts.METADATA_TYPE_MEMENTO:
                    headerString += ",Team,Year,Position,FanPoints,ModelType,GameDate,Opponent,HomeTeam";
                    break;
                case Consts.METADATA_TYPE_SPIRITHLWN:
                    headerString += ",Rarity";
                    break;
                case Consts.METADATA_TYPE_STRUCTORNMT:
                    headerString += ",BuildingType,Rarity";
                    break;
                default:
                    break;
            }
            headerString += ",Link";
            csvString.Add(headerString);

            foreach (WebNFT nft in nfts)
            {
                string entryString = string.Format("{0},{1},{2},{3},{4},{5}", nft.DGoodId, nft.SerialNumber, nft.CurrentSupply, nft.MaxSupply, nft.Name, nft.Owner);
                switch (category)
                {
                    case Consts.METADATA_TYPE_BLKEXPLORER:
                        entryString += string.Format(",{0},{1}", nft.SeriesName, nft.Rarity);
                        break;
                    case Consts.METADATA_TYPE_ESSENTIAL:
                        entryString += string.Format(",{0},{1},{2},{3},{4},{5}", nft.Team, nft.Year, nft.Position, nft.FanPoints, nft.ModelType, nft.IsVariant);
                        break;
                    case Consts.METADATA_TYPE_MEMENTO:
                        entryString += string.Format(",{0},{1},{2},{3},{4},{5}", nft.Team, nft.Year, nft.Position, nft.FanPoints, nft.ModelType, nft.GameDate.ToString("MMMM dd yyyy"), nft.Opponent, nft.HomeTeam);
                        break;
                    case Consts.METADATA_TYPE_SPIRITHLWN:
                        entryString += string.Format(",{0}", nft.Rarity);
                        break;
                    case Consts.METADATA_TYPE_STRUCTORNMT:
                        entryString += string.Format(",{0},{1}", nft.BuildingType, nft.Rarity);
                        break;
                    default:
                        break;
                }
                entryString += string.Format(",{0}", nft.Link);

                csvString.Add(entryString);
            }

            return csvString;
        }

        #region NFT Specific Sorting

        private List<WebNFT> FilterAndSortBlockExplorerNFTs(List<NFT> matchingNfts, WebNFTFilters filters)
        {
            List<WebNFT> webNfts = new List<WebNFT>();

            Dictionary<int, BlockExplorerMetadata> metadataDictionary = _cachingProcessor.GetBlockExplorerMetadataFromCache();
            Dictionary<int, int> nftCountDictionary = _cachingProcessor.GetCurrentNFTCountsFromCache();

            matchingNfts = matchingNfts.Where(n => metadataDictionary.ContainsKey(n.NFTMetadataId)).ToList();

            foreach (NFT nft in matchingNfts)
            {
                BlockExplorerSpecificMetadata nftMetadata = HelperFunctions.DecodeMetadata<BlockExplorerSpecificMetadata>(nft.Metadata);
                webNfts.Add(new WebNFT
                {
                    DGoodId = nft.DGoodId,
                    Image = metadataDictionary[nft.NFTMetadataId].Image,
                    Link = nftMetadata.Link,
                    SerialNumber = nft.SerialNumber,
                    Name = metadataDictionary[nft.NFTMetadataId].DisplayName,
                    Owner = nft.Owner,
                    MaxSupply = metadataDictionary[nft.NFTMetadataId].MaxSupply,
                    CurrentSupply = nftCountDictionary[nft.NFTMetadataId],

                    Description = metadataDictionary[nft.NFTMetadataId].Description,
                    SeriesId = metadataDictionary[nft.NFTMetadataId].SeriesId,
                    SeriesName = metadataDictionary[nft.NFTMetadataId].SeriesName,
                    Rarity = metadataDictionary[nft.NFTMetadataId]?.RarityLevel == null ? "" : metadataDictionary[nft.NFTMetadataId].RarityLevel,

                    Category = Consts.METADATA_TYPE_BLKEXPLORER
                });
            }

            webNfts = webNfts
                .Where(n =>
                    (string.IsNullOrWhiteSpace(filters.Filters.Owner) || n.Owner.IndexOf(filters.Filters.Owner, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            if (filters.SortDescending)
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderByDescending(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderByDescending(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderByDescending(m => m.Owner).ToList();
                }
            }
            else
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderBy(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderBy(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderBy(m => m.Owner).ToList();
                }
            }

            return webNfts;
        }

        private List<WebNFT> FilterAndSortStructureOrnamentNFTs(List<NFT> matchingNfts, WebNFTFilters filters)
        {
            List<WebNFT> webNfts = new List<WebNFT>();

            Dictionary<int, StructornmtMetadata> metadataDictionary = _cachingProcessor.GetStructornmtMetadataFromCache();
            Dictionary<int, int> nftCountDictionary = _cachingProcessor.GetCurrentNFTCountsFromCache();

            matchingNfts = matchingNfts.Where(n => metadataDictionary.ContainsKey(n.NFTMetadataId)).ToList();

            foreach (NFT nft in matchingNfts)
            {
                BlockExplorerSpecificMetadata nftMetadata = HelperFunctions.DecodeMetadata<BlockExplorerSpecificMetadata>(nft.Metadata);
                webNfts.Add(new WebNFT
                {
                    DGoodId = nft.DGoodId,
                    Image = metadataDictionary[nft.NFTMetadataId].Image,
                    Link = nftMetadata.Link,
                    SerialNumber = nft.SerialNumber,
                    Name = metadataDictionary[nft.NFTMetadataId].DisplayName,
                    Owner = nft.Owner,
                    MaxSupply = metadataDictionary[nft.NFTMetadataId].MaxSupply,
                    CurrentSupply = nftCountDictionary[nft.NFTMetadataId],

                    BuildingType = metadataDictionary[nft.NFTMetadataId].BuildingType,
                    Rarity = metadataDictionary[nft.NFTMetadataId]?.RarityLevel == null ? "" : metadataDictionary[nft.NFTMetadataId].RarityLevel,

                    Category = Consts.METADATA_TYPE_STRUCTORNMT
                });
            }

            webNfts = webNfts
                .Where(n =>
                    (string.IsNullOrWhiteSpace(filters.Filters.Owner) || n.Owner.IndexOf(filters.Filters.Owner, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            if (filters.SortDescending)
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderByDescending(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderByDescending(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderByDescending(m => m.Owner).ToList();
                }
            }
            else
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderBy(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderBy(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderBy(m => m.Owner).ToList();
                }
            }

            return webNfts;
        }

        private List<WebNFT> FilterAndSortSpiritHlwnNFTs(List<NFT> matchingNfts, WebNFTFilters filters)
        {
            List<WebNFT> webNfts = new List<WebNFT>();

            Dictionary<int, SpirithlwnMetadata> metadataDictionary = _cachingProcessor.GetSpiritHlwnMetadataFromCache();
            Dictionary<int, int> nftCountDictionary = _cachingProcessor.GetCurrentNFTCountsFromCache();

            matchingNfts = matchingNfts.Where(n => metadataDictionary.ContainsKey(n.NFTMetadataId)).ToList();

            foreach (NFT nft in matchingNfts)
            {
                SpirithlwnSpecificMetadata nftMetadata = HelperFunctions.DecodeMetadata<SpirithlwnSpecificMetadata>(nft.Metadata);
                webNfts.Add(new WebNFT
                {
                    DGoodId = nft.DGoodId,
                    Image = metadataDictionary[nft.NFTMetadataId].Image,
                    Link = nftMetadata.Link,
                    SerialNumber = nft.SerialNumber,
                    Name = metadataDictionary[nft.NFTMetadataId].DisplayName,
                    Owner = nft.Owner,
                    MaxSupply = metadataDictionary[nft.NFTMetadataId].MaxSupply,
                    CurrentSupply = nftCountDictionary[nft.NFTMetadataId],

                    Rarity = metadataDictionary[nft.NFTMetadataId]?.RarityLevel == null ? "" : metadataDictionary[nft.NFTMetadataId].RarityLevel,

                    Category = Consts.METADATA_TYPE_SPIRITHLWN
                });
            }

            webNfts = webNfts
                .Where(n =>
                    (string.IsNullOrWhiteSpace(filters.Filters.Owner) || n.Owner.IndexOf(filters.Filters.Owner, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            if (filters.SortDescending)
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderByDescending(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderByDescending(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderByDescending(m => m.Owner).ToList();
                }
            }
            else
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderBy(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderBy(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderBy(m => m.Owner).ToList();
                }
            }

            return webNfts;
        }

        private List<WebNFT> FilterAndSortEssentialNFTs(List<NFT> matchingNfts, WebNFTFilters filters)
        {
            List<WebNFT> webNfts = new List<WebNFT>();

            Dictionary<int, EssentialMetadata> metadataDictionary = _cachingProcessor.GetEssentialMetadataFromCache();
            Dictionary<int, int> nftCountDictionary = _cachingProcessor.GetCurrentNFTCountsFromCache();

            matchingNfts = matchingNfts.Where(n => metadataDictionary.ContainsKey(n.NFTMetadataId)).ToList();

            foreach (NFT nft in matchingNfts)
            {
                EssentialSpecificMetadata nftMetadata = HelperFunctions.DecodeMetadata<EssentialSpecificMetadata>(nft.Metadata);
                webNfts.Add(new WebNFT
                {
                    DGoodId = nft.DGoodId,
                    Image = metadataDictionary[nft.NFTMetadataId].Image,
                    Link = nftMetadata.Link,
                    SerialNumber = nft.SerialNumber,
                    Name = metadataDictionary[nft.NFTMetadataId].PlayerFullName,
                    Owner = nft.Owner,
                    MaxSupply = metadataDictionary[nft.NFTMetadataId].MaxSupply,
                    CurrentSupply = nftCountDictionary[nft.NFTMetadataId],

                    Team = metadataDictionary[nft.NFTMetadataId]?.TeamName,
                    IsVariant = nftMetadata.IsVariant,
                    Year = metadataDictionary[nft.NFTMetadataId].Season.ToString(),
                    Position = metadataDictionary[nft.NFTMetadataId].PlayerPosition,
                    FanPoints = metadataDictionary[nft.NFTMetadataId].FanPoints,
                    ModelType = metadataDictionary[nft.NFTMetadataId].ModelType,

                    Category = Consts.METADATA_TYPE_ESSENTIAL
                });
            }

            webNfts = webNfts
                .Where(n =>
                    (string.IsNullOrWhiteSpace(filters.Filters.Owner) || n.Owner.IndexOf(filters.Filters.Owner, StringComparison.OrdinalIgnoreCase) >= 0)
                    && (filters.Filters.IsVariantFilter == 0 || filters.Filters.IsVariantFilter == 1 && n.IsVariant || filters.Filters.IsVariantFilter == 2 && !n.IsVariant))
                .ToList();

            if (filters.SortDescending)
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderByDescending(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "FanPoints")
                {
                    webNfts = webNfts.OrderByDescending(m => m.FanPoints).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderByDescending(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderByDescending(m => m.Owner).ToList();
                }
            }
            else
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderBy(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "FanPoints")
                {
                    webNfts = webNfts.OrderBy(m => m.FanPoints).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderBy(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderBy(m => m.Owner).ToList();
                }
            }

            return webNfts;
        }

        private List<WebNFT> FilterAndSortMementosNFTs(List<NFT> matchingNfts, WebNFTFilters filters)
        {
            List<WebNFT> webNfts = new List<WebNFT>();

            Dictionary<int, MementoMetadata> metadataDictionary = _cachingProcessor.GetMementoMetadataFromCache();
            Dictionary<int, int> nftCountDictionary = _cachingProcessor.GetCurrentNFTCountsFromCache();

            matchingNfts = matchingNfts.Where(n => metadataDictionary.ContainsKey(n.NFTMetadataId)).ToList();

            foreach (NFT nft in matchingNfts)
            {
                MementoSpecificMetadata nftMetadata = HelperFunctions.DecodeMetadata<MementoSpecificMetadata>(nft.Metadata);
                webNfts.Add(new WebNFT
                {
                    DGoodId = nft.DGoodId,
                    Image = metadataDictionary[nft.NFTMetadataId].Image,
                    Link = nftMetadata.Link,
                    SerialNumber = nft.SerialNumber,
                    Name = metadataDictionary[nft.NFTMetadataId].PlayerFullName,
                    Owner = nft.Owner,
                    MaxSupply = metadataDictionary[nft.NFTMetadataId].MaxSupply,
                    CurrentSupply = nftCountDictionary[nft.NFTMetadataId],

                    Team = metadataDictionary[nft.NFTMetadataId]?.TeamName,
                    Year = metadataDictionary[nft.NFTMetadataId].Season.ToString(),
                    Position = metadataDictionary[nft.NFTMetadataId].PlayerPosition,
                    FanPoints = metadataDictionary[nft.NFTMetadataId].FanPoints,
                    ModelType = metadataDictionary[nft.NFTMetadataId].ModelType,
                    GameDate = nftMetadata.GameDate,
                    Opponent = nftMetadata.OpponentTeam,
                    HomeTeam = nftMetadata.HomeTeam,

                    Category = Consts.METADATA_TYPE_MEMENTO
                });
            }

            webNfts = webNfts
                .Where(n =>
                    (string.IsNullOrWhiteSpace(filters.Filters.Opponent) || n.Opponent.IndexOf(filters.Filters.Opponent, StringComparison.OrdinalIgnoreCase) >= 0)
                    && (string.IsNullOrWhiteSpace(filters.Filters.HomeTeam) || n.HomeTeam.IndexOf(filters.Filters.HomeTeam, StringComparison.OrdinalIgnoreCase) >= 0)
                    && (string.IsNullOrWhiteSpace(filters.Filters.Owner) || n.Owner.IndexOf(filters.Filters.Owner, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            if (filters.SortDescending)
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderByDescending(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "FanPoints")
                {
                    webNfts = webNfts.OrderByDescending(m => m.FanPoints).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderByDescending(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderByDescending(m => m.Owner).ToList();
                }
            }
            else
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderBy(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "FanPoints")
                {
                    webNfts = webNfts.OrderBy(m => m.FanPoints).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderBy(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderBy(m => m.Owner).ToList();
                }
            }

            return webNfts;
        }

        private List<WebNFT> FilterAndSortStructureNFTs(List<NFT> matchingNfts, WebNFTFilters filters)
        {
            List<WebNFT> webNfts = new List<WebNFT>();

            Dictionary<int, StructureMetadata> metadataDictionary = _cachingProcessor.GetStructureMetadataFromCache();
            Dictionary<int, int> nftCountDictionary = _cachingProcessor.GetCurrentNFTCountsFromCache();

            matchingNfts = matchingNfts.Where(n => metadataDictionary.ContainsKey(n.NFTMetadataId)).ToList();

            foreach (NFT nft in matchingNfts)
            {
                StructureSpecificMetaData nftMetadata = HelperFunctions.DecodeMetadata<StructureSpecificMetaData>(nft.Metadata);
                webNfts.Add(new WebNFT
                {
                    DGoodId = nft.DGoodId,
                    Image = metadataDictionary[nft.NFTMetadataId].Image,
                    Link = string.Format("https://play.upland.me/?prop_id={0}", nftMetadata.PropertyId),
                    SerialNumber = 0,
                    Name = metadataDictionary[nft.NFTMetadataId].DisplayName,
                    Owner = nft.Owner,
                    MaxSupply = 0,
                    CurrentSupply = nftCountDictionary[nft.NFTMetadataId],

                    Category = Consts.METADATA_TYPE_STRUCTURE
                });
            }

            webNfts = webNfts
                .Where(n =>
                    (string.IsNullOrWhiteSpace(filters.Filters.Owner) || n.Owner.IndexOf(filters.Filters.Owner, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToList();

            if (filters.SortDescending)
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderByDescending(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderByDescending(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderByDescending(m => m.Owner).ToList();
                }
            }
            else
            {
                if (filters.SortBy == "Mint")
                {
                    webNfts = webNfts.OrderBy(m => m.SerialNumber).ToList();
                }
                else if (filters.SortBy == "Name")
                {
                    webNfts = webNfts.OrderBy(m => m.Name).ToList();
                }
                else
                {
                    webNfts = webNfts.OrderBy(m => m.Owner).ToList();
                }
            }

            return webNfts;
        }
        #endregion
    }
}