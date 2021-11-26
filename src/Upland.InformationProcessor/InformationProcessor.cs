using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upland.Infrastructure.Blockchain;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public class InformationProcessor
    {
        private readonly LocalDataManager localDataManager;
        private readonly UplandApiManager uplandApiManager;
        private readonly BlockchainManager blockchainManager;

        public InformationProcessor()
        {
            localDataManager = new LocalDataManager();
            uplandApiManager = new UplandApiManager();
            blockchainManager = new BlockchainManager();
        }

        public List<string> GetCollectionInformation(string fileType)
        {
            List<string> output = new List<string>();

            List<CollatedStatsObject> collectionStats = localDataManager.GetCollectionStats();
            List<Collection> collections = localDataManager.GetCollections();
            collections = collections.OrderBy(c => c.Category).OrderBy(c => c.CityId).ToList();

            if (fileType == "TXT")
            {
                int idPad = 8;
                int categoryPad = 10;
                int NamePad = collections.OrderByDescending(c => c.Name.Length).First().Name.Length;
                int boostPad = 5;
                int slotsPad = 5;
                int rewardPad = 7;
                int propCountPad = 11;


                output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6} - Locked Props - Unlocked Non-FSA Props - Unlocked FSA Props - For Sale Props - Owned Props - Percent Minted - Percent Non-FSA Minted"
                    , "Id".PadLeft(idPad)
                    , "Category".PadLeft(categoryPad)
                    , "Name".PadLeft(NamePad)
                    , "Boost".PadLeft(boostPad)
                    , "Slots".PadLeft(slotsPad)
                    , "Reward".PadLeft(rewardPad)
                    , "Prop Count".PadLeft(propCountPad)));

                output.Add("");
                output.Add("Standard Collections");

                int? cityId = -1;
                foreach (Collection collection in collections)
                {
                    if (cityId != collection.CityId)
                    {
                        cityId = collection.CityId;
                        output.Add("");
                        output.Add(Consts.Cities[cityId.Value]);
                    }

                    string collectionString = string.Format("{0} - {1} - {2} - {3} - {4} - {5} - "
                        , collection.Id.ToString().PadLeft(idPad)
                        , HelperFunctions.GetCollectionCategory(collection.Category).PadRight(categoryPad)
                        , collection.Name.PadLeft(NamePad)
                        , string.Format("{0:N2}", collection.Boost).PadLeft(boostPad)
                        , collection.NumberOfProperties.ToString().PadLeft(slotsPad)
                        , string.Format("{0:N0}", collection.Reward).ToString().PadLeft(rewardPad)
                    );

                    if (!collectionStats.Any(c => c.Id == collection.Id))
                    {
                        collectionString += HelperFunctions.CreateCollatedStatTextString(new CollatedStatsObject
                        {
                            TotalProps = 0,
                            LockedProps = 0,
                            UnlockedNonFSAProps = 0,
                            UnlockedFSAProps = 0,
                            ForSaleProps = 0,
                            OwnedProps = 0,
                            PercentMinted = 100.00,
                            PercentNonFSAMinted = 100.00
                        });
                    }
                    else
                    {
                        CollatedStatsObject stats = collectionStats.Where(c => c.Id == collection.Id).First();
                        collectionString += HelperFunctions.CreateCollatedStatTextString(stats);
                    }

                    output.Add(collectionString);
                }
            }
            else
            {
                output.Add("Id,Category,Name,Boost,Slots,Reward,NumberOfProperties,LockedProps,UnlockedNonFSAProps,UnlockedFSAProps,ForSaleProps,OwnedProps,PercentMinted,PercentNonFSAMinted");
                foreach (Collection collection in collections)
                {
                    string collectionString = string.Format("{0},{1},{2},{3},{4},{5},"
                        , collection.Id.ToString()
                        , HelperFunctions.GetCollectionCategory(collection.Category)
                        , collection.Name
                        , collection.Boost
                        , collection.NumberOfProperties
                        , string.Format("{0}", collection.Reward).ToString()
                    );

                    if (!collectionStats.Any(c => c.Id == collection.Id))
                    {
                        collectionString += HelperFunctions.CreateCollatedStatCSVString(new CollatedStatsObject
                        {
                            TotalProps = 0,
                            LockedProps = 0,
                            UnlockedNonFSAProps = 0,
                            UnlockedFSAProps = 0,
                            ForSaleProps = 0,
                            OwnedProps = 0,
                            PercentMinted = 100.00,
                            PercentNonFSAMinted = 100.00
                        });
                    }
                    else
                    {
                        CollatedStatsObject stats = collectionStats.Where(c => c.Id == collection.Id).First();
                        collectionString += HelperFunctions.CreateCollatedStatCSVString(stats);
                    }

                    output.Add(collectionString);
                }
            }

            return output;
        }

        public List<string> GetNeighborhoodInformation(string fileType)
        {
            List<string> output = new List<string>();
            List<CollatedStatsObject> neighborhoodStats = localDataManager.GetNeighborhoodStats();

            List<Neighborhood> neighborhoods = localDataManager.GetNeighborhoods();
            neighborhoods = neighborhoods.OrderBy(n => n.Name).OrderBy(n => n.CityId).ToList();

            if (fileType == "TXT")
            {
                int idPad = 9;
                int namePad = neighborhoods.OrderByDescending(n => n.Name.Length).First().Name.Length;

                output.Add(string.Format("{0} - {1} - Total Props - Locked Props - Unlocked Non-FSA Props - Unlocked FSA Props - For Sale Props - Owned Props - Percent Minted - Percent Non-FSA Minted", "Id".PadLeft(idPad), "Name".PadLeft(namePad)));
                output.Add("");

                int cityId = -1;
                foreach (Neighborhood neighborhood in neighborhoods)
                {
                    if (cityId != neighborhood.CityId)
                    {
                        cityId = neighborhood.CityId;
                        output.Add("");
                        output.Add(Consts.Cities[cityId]);
                    }

                    string neighborhoodString = string.Format("{0} - {1}"
                        , neighborhood.Id.ToString().PadLeft(idPad)
                        , neighborhood.Name.PadLeft(namePad)
                    );

                    if (!neighborhoodStats.Any(n => n.Id == neighborhood.Id))
                    {
                        neighborhoodString += HelperFunctions.CreateCollatedStatTextString(new CollatedStatsObject
                        {
                            TotalProps = 0,
                            LockedProps = 0,
                            UnlockedNonFSAProps = 0,
                            UnlockedFSAProps = 0,
                            ForSaleProps = 0,
                            OwnedProps = 0,
                            PercentMinted = 100.00,
                            PercentNonFSAMinted = 100.00
                        });
                    }
                    else
                    {
                        CollatedStatsObject stats = neighborhoodStats.Where(n => n.Id == neighborhood.Id).First();
                        neighborhoodString += HelperFunctions.CreateCollatedStatTextString(stats);
                    }

                    output.Add(neighborhoodString);
                }
            }
            else
            {
                output.Add("Id,Name,CityId,TotalProps,LockedProps,UnlockedNonFSAProps,UnlockedFSAProps,ForSaleProps,OwnedProps,PercentMinted,PercentNonFSAMinted");
                foreach (Neighborhood neighborhood in neighborhoods)
                {
                    string neighborhoodString = string.Format("{0},{1},{2},"
                        , neighborhood.Id.ToString()
                        , neighborhood.Name.Replace(',', ' ')
                        , neighborhood.CityId
                    );

                    if (!neighborhoodStats.Any(n => n.Id == neighborhood.Id))
                    {
                        neighborhoodString += HelperFunctions.CreateCollatedStatCSVString(new CollatedStatsObject
                        {
                            TotalProps = 0,
                            LockedProps = 0,
                            UnlockedNonFSAProps = 0,
                            UnlockedFSAProps = 0,
                            ForSaleProps = 0,
                            OwnedProps = 0,
                            PercentMinted = 100.00,
                            PercentNonFSAMinted = 100.00
                        });
                    }
                    else
                    {
                        CollatedStatsObject stats = neighborhoodStats.Where(n => n.Id == neighborhood.Id).First();
                        neighborhoodString += HelperFunctions.CreateCollatedStatCSVString(stats);
                    }

                    output.Add(neighborhoodString);
                }
            }

            return output;
        }

        public List<string> GetStreetInformation(string fileType)
        {
            List<string> output = new List<string>();
            List<CollatedStatsObject> streetStats = localDataManager.GetStreetStats();

            List<Street> streets = localDataManager.GetStreets();
            streets = streets.OrderBy(n => n.Name).OrderBy(n => n.CityId).ToList();

            if (fileType == "TXT")
            {
                int idPad = 5;
                int cityPad = 6;
                int typePad = 14;
                int namePad = streets.OrderByDescending(n => n.Name.Length).First().Name.Length;

                output.Add(string.Format("{0} - {1} - {2} - {3} - Total Props - Locked Props - Unlocked Non-FSA Props - Unlocked FSA Props - For Sale Props - Owned Props - Percent Minted - Percent Non-FSA Minted", "Id".PadLeft(idPad), "CityId".PadLeft(cityPad), "Name".PadLeft(namePad), "Type".PadLeft(typePad)));
                output.Add("");

                int cityId = -1;
                foreach (Street street in streets)
                {
                    if (cityId != street.CityId)
                    {
                        cityId = street.CityId;
                        output.Add("");
                        output.Add(Consts.Cities[cityId]);
                    }

                    string streetString = string.Format("{0} - {1} - {2} - {3}"
                        , street.Id.ToString().PadLeft(idPad)
                        , street.CityId.ToString().PadLeft(cityPad)
                        , street.Name.PadLeft(namePad)
                        , street.Type.PadLeft(typePad)
                    );

                    if (!streetStats.Any(s => s.Id == street.Id))
                    {
                        streetString += HelperFunctions.CreateCollatedStatTextString(new CollatedStatsObject
                        {
                            TotalProps = 0,
                            LockedProps = 0,
                            UnlockedNonFSAProps = 0,
                            UnlockedFSAProps = 0,
                            ForSaleProps = 0,
                            OwnedProps = 0,
                            PercentMinted = 100.00,
                            PercentNonFSAMinted = 100.00
                        });
                    }
                    else
                    {
                        CollatedStatsObject stats = streetStats.Where(s => s.Id == street.Id).First();
                        streetString += HelperFunctions.CreateCollatedStatTextString(stats);
                    }

                    output.Add(streetString);
                }
            }
            else
            {
                output.Add("Id,CityId,Name,Type,CityId,TotalProps,LockedProps,UnlockedNonFSAProps,UnlockedFSAProps,ForSaleProps,OwnedProps,PercentMinted,PercentNonFSAMinted");
                foreach (Street street in streets)
                {
                    string streetString = string.Format("{0},{1},{2},{3},"
                        , street.Id.ToString()
                        , street.CityId.ToString()
                        , street.Name.Replace(',', ' ')
                        , street.Type
                    );

                    if (!streetStats.Any(s => s.Id == street.Id))
                    {
                        streetString += HelperFunctions.CreateCollatedStatCSVString(new CollatedStatsObject
                        {
                            TotalProps = 0,
                            LockedProps = 0,
                            UnlockedNonFSAProps = 0,
                            UnlockedFSAProps = 0,
                            ForSaleProps = 0,
                            OwnedProps = 0,
                            PercentMinted = 100.00,
                            PercentNonFSAMinted = 100.00
                        });
                    }
                    else
                    {
                        CollatedStatsObject stats = streetStats.Where(s => s.Id == street.Id).First();
                        streetString += HelperFunctions.CreateCollatedStatCSVString(stats);
                    }

                    output.Add(streetString);
                }
            }

            return output;
        }

        public List<string> GetCityInformation(string fileType)
        {
            List<string> array = new List<string>();
            List<CollatedStatsObject> cityStats = localDataManager.GetCityStats();

            if (fileType == "TXT")
            {
                int idPad = 5;
                int namePad = Consts.Cities.OrderByDescending(c => c.Value.Length).First().Value.Length;

                array.Add(string.Format("{0} - {1} - Total Props - Locked Props - Unlocked Non-FSA Props - Unlocked FSA Props - For Sale Props - Owned Props - Percent Minted - Percent Non-FSA Minted", "Id".PadLeft(idPad), "Name".PadLeft(namePad)));

                foreach (int cityId in Consts.Cities.Keys.Where(k => k < 10000))
                {
                    string cityString = string.Format("{0} - {1} -", cityId.ToString().PadLeft(idPad), Consts.Cities[cityId].PadLeft(namePad));

                    if (!cityStats.Any(c => c.Id == cityId))
                    {
                        cityString += HelperFunctions.CreateCollatedStatTextString(new CollatedStatsObject
                        {
                            TotalProps = 0,
                            LockedProps = 0,
                            UnlockedNonFSAProps = 0,
                            UnlockedFSAProps = 0,
                            ForSaleProps = 0,
                            OwnedProps = 0,
                            PercentMinted = 100.00,
                            PercentNonFSAMinted = 100.00
                        });
                    }
                    else
                    {
                        CollatedStatsObject stats = cityStats.Where(c => c.Id == cityId).First();
                        cityString += HelperFunctions.CreateCollatedStatTextString(stats);
                    }
                    array.Add(cityString);
                }
            }
            else
            {
                array.Add("Id,Name,TotalProps,LockedProps,UnlockedNonFSAProps,UnlockedFSAProps,ForSaleProps,OwnedProps,PercentMinted,PercentNonFSAMinted");
                foreach (int cityId in Consts.Cities.Keys)
                {
                    string entry = string.Format("{0},{1},", cityId, Consts.Cities[cityId]);

                    if (!cityStats.Any(c => c.Id == cityId))
                    {
                        entry += HelperFunctions.CreateCollatedStatCSVString(new CollatedStatsObject
                        {
                            TotalProps = 0,
                            LockedProps = 0,
                            UnlockedNonFSAProps = 0,
                            UnlockedFSAProps = 0,
                            ForSaleProps = 0,
                            OwnedProps = 0,
                            PercentMinted = 100.00,
                            PercentNonFSAMinted = 100.00
                        });
                    }
                    else
                    {
                        CollatedStatsObject stats = cityStats.Where(c => c.Id == cityId).First();
                        entry += HelperFunctions.CreateCollatedStatCSVString(stats);
                    }
                    array.Add(entry);
                }
            }
            return array;
        }

        public async Task<List<string>> GetPropertyInfo(string username, string fileType)
        {
            List<string> output = new List<string>();
            List<Property> properties = await localDataManager.GetPropertysByUsername(username);

            properties = properties.OrderBy(p => p.Address).OrderBy(p => p.CityId).ToList();

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);
            
            if (fileType == "CSV")
            {
                output.Add("PropertyId,Size,Mint,NeighborhoodId,CityId,Address,Structure");

                foreach (Property property in properties)
                {
                    string propString = "";

                    propString += string.Format("{0},", property.Id);
                    propString += string.Format("{0},", property.Size);
                    propString += string.Format("{0:F0},", Math.Round(property.MonthlyEarnings * 12 / 0.1728));
                    propString += string.Format("{0},", property.NeighborhoodId.HasValue ? property.NeighborhoodId.Value.ToString() : "-1");
                    propString += string.Format("{0},", property.CityId);
                    propString += string.Format("{0},", property.Address);
                    propString += string.Format("{0}", propertyStructures.ContainsKey(property.Id) ? propertyStructures[property.Id] : "None");

                    output.Add(propString);
                }
            }
            else
            {
                int idPad = 19;
                int sizePad = 7;
                int mintPad = 12;
                int neighborhoodPad = 14;
                int addressPad = properties.Max(p => p.Address.Length);
                int buildingPad = 29;

                output.Add(string.Format("Property Information For {0} as of {1:MM/dd/yy H:mm:ss}", username.ToUpper(), DateTime.Now));
                output.Add("");
                output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5}"
                    , "Id".PadLeft(idPad)
                    , "Size".PadLeft(sizePad)
                    , "Mint".PadLeft(mintPad)
                    , "NeighborhoodId".PadLeft(neighborhoodPad)
                    , "Address".PadLeft(addressPad)
                    , "Building".PadLeft(buildingPad)));

                int? cityId = -1;
                foreach (Property property in properties)
                {
                    if (cityId != property.CityId)
                    {
                        cityId = property.CityId;
                        output.Add("");
                        output.Add(Consts.Cities[cityId.Value]);
                    }

                    output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5}"
                        , property.Id.ToString().PadLeft(idPad)
                        , string.Format("{0:N0}", property.Size).PadLeft(sizePad)
                        , string.Format("{0:N2}", Math.Round(property.MonthlyEarnings * 12 / 0.1728)).ToString().PadLeft(mintPad)
                        , string.Format("{0}", property.NeighborhoodId.HasValue ? property.NeighborhoodId.Value.ToString() : "-1").PadLeft(neighborhoodPad)
                        , property.Address.PadLeft(addressPad)
                        , string.Format("{0}", propertyStructures.ContainsKey(property.Id) ? propertyStructures[property.Id] : "None").PadLeft(buildingPad)
                    ));
                }
            }

            return output;
        }

        public List<string> SearchStreets(string name, string fileType)
        {
            List<string> output = new List<string>();
            List<Street> streets = localDataManager.SearchStreets(name);

            if (streets.Count == 0)
            {
                output.Add(string.Format("Sorry, No Streets Found for {0}.", name));
                return output;
            }

            streets = streets.OrderBy(n => n.Name).OrderBy(n => n.CityId).ToList();

            if (fileType == "TXT")
            {
                int idPad = 5;
                int cityPad = 6;
                int typePad = 14;
                int namePad = streets.OrderByDescending(n => n.Name.Length).First().Name.Length;

                output.Add(string.Format("{0} - {1} - {2} - {3}", "Id".PadLeft(idPad), "CityId".PadLeft(cityPad), "Name".PadLeft(namePad), "Type".PadLeft(typePad)));
                output.Add("");

                foreach (Street street in streets)
                {
                    string streetString = string.Format("{0} - {1} - {2} - {3}"
                        , street.Id.ToString().PadLeft(idPad)
                        , street.CityId.ToString().PadLeft(cityPad)
                        , street.Name.PadLeft(namePad)
                        , street.Type.PadLeft(typePad)
                    );

                    output.Add(streetString);
                }
            }
            else
            {
                output.Add("Id,CityId,Name,Type");
                foreach (Street street in streets)
                {
                    string streetString = string.Format("{0},{1},{2},{3},"
                        , street.Id.ToString()
                        , street.CityId.ToString()
                        , street.Name.Replace(',', ' ')
                        , street.Type
                    );

                    output.Add(streetString);
                }
            }

            return output;
        }

        public async Task<List<string>> GetCollectionPropertiesForSale(int collectionId, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            List<Collection> collections = localDataManager.GetCollections();

            if (!collections.Any(c => c.Id == collectionId))
            {
                // Collection don't exist
                output.Add(string.Format("{0} is not a valid collectionId. Try running my !CollectionInfo command.", collectionId.ToString()));
                return output;
            }

            if (collections.Any(c => c.Id == collectionId && c.IsCityCollection))
            {
                // Don't do city collections
                output.Add(string.Format("This doesn't work for city collections.", collectionId.ToString()));
                return output;
            }

            Collection collection = collections.Where(c => c.Id == collectionId).First();
            List<UplandForSaleProp> forSaleProps = await uplandApiManager.GetForSalePropsByCityId(collection.CityId.Value);

            if (currency == "USD")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "USD" && collection.MatchingPropertyIds.Contains(p.Prop_Id)).ToList();
            }
            else if (currency == "UPX")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "UPX" && collection.MatchingPropertyIds.Contains(p.Prop_Id)).ToList();
            }
            else
            {
                forSaleProps = forSaleProps.Where(p => collection.MatchingPropertyIds.Contains(p.Prop_Id)).ToList();
            }

            if (forSaleProps.Count == 0)
            {
                // Nothing on sale
                output.Add(string.Format("There is nothing on sale in this collection.", collectionId.ToString()));
                return output;
            }

            Dictionary<long, Property> properties = localDataManager.GetPropertiesByCollectionId(collectionId).ToDictionary(p => p.Id, p => p);

            if(orderBy == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].MonthlyEarnings * 12/0.1728).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, properties, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report for {0}", collection.Name), uplandApiManager.GetCacheDateTime(collection.CityId.Value)));
            }

            return output;
        }

        public async Task<List<string>> GetNeighborhoodPropertiesForSale(int neighborhoodId, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            List<Neighborhood> neighborhoods = localDataManager.GetNeighborhoods();

            if (!neighborhoods.Any(n => n.Id == neighborhoodId))
            {
                // Neighborhood don't exist
                output.Add(string.Format("{0} is not a valid neighborhoodId. Try running my !NeighborhoodInfo command.", neighborhoodId.ToString()));
                return output;
            }

            Neighborhood neighborhood = neighborhoods.Where(n => n.Id == neighborhoodId).First();
            List<UplandForSaleProp> forSaleProps = await uplandApiManager.GetForSalePropsByCityId(neighborhood.CityId);

            if (currency == "USD")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "USD").ToList();
            }
            else if (currency == "UPX")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "UPX").ToList();
            }

            Dictionary<long, Property> properties = localDataManager.GetPropertiesByCityId(neighborhood.CityId).ToDictionary(p => p.Id, p => p);

            forSaleProps = forSaleProps.Where(p => properties.ContainsKey(p.Prop_Id) && properties[p.Prop_Id].NeighborhoodId == neighborhoodId).ToList();

            if (forSaleProps.Count == 0)
            {
                // Nothing on sale
                output.Add(string.Format("There is nothing on sale in this neighborhood.", neighborhoodId.ToString()));
                return output;
            }

            if (orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].MonthlyEarnings * 12 / 0.1728).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, properties, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report for {0}", neighborhood.Name), uplandApiManager.GetCacheDateTime(neighborhood.CityId)));
            }

            return output;
        }

        public async Task<List<string>> GetBuildingPropertiesForSale(string type, int Id, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();
            List<UplandForSaleProp> forSaleProps = new List<UplandForSaleProp>();
            Dictionary<long, Property> properties = new Dictionary<long, Property>();
            int cityId = 0;

            if (type.ToUpper() == "CITY")
            {
                cityId = Id;
                if (Id != 0 && !Consts.Cities.ContainsKey(Id))
                {
                    output.Add(string.Format("{0} is not a valid cityId. Try running my !CityInfo command.", Id));
                    return output;
                }

                if (cityId == 0)
                {
                    List<Property> allProps = new List<Property>();
                    foreach (int cId in Consts.Cities.Keys)
                    {
                        allProps.AddRange(localDataManager.GetPropertiesByCityId(cId));
                        forSaleProps.AddRange(await uplandApiManager.GetForSalePropsByCityId(cId));
                    }

                    properties = allProps.ToDictionary(p => p.Id, p => p);
                    forSaleProps = forSaleProps.GroupBy(p => p.Prop_Id)
                        .Select(g => g.First())
                        .ToList();
                }
                else
                {
                    properties = localDataManager.GetPropertiesByCityId(cityId).ToDictionary(p => p.Id, p => p);
                    forSaleProps = await uplandApiManager.GetForSalePropsByCityId(cityId);
                }

                forSaleProps = forSaleProps.Where(p => properties.ContainsKey(p.Prop_Id)).ToList();
                cityId = 1; // Fix for Sales Cache time, just use San Francisco
            }
            else if(type.ToUpper() == "NEIGHBORHOOD")
            {
                List<Neighborhood> neighborhoods = localDataManager.GetNeighborhoods();

                if (!neighborhoods.Any(n => n.Id == Id))
                {
                    // Neighborhood don't exist
                    output.Add(string.Format("{0} is not a valid neighborhoodId. Try running my !NeighborhoodInfo command.", Id.ToString()));
                    return output;
                }

                Neighborhood neighborhood = neighborhoods.Where(n => n.Id == Id).First();
                cityId = neighborhood.CityId;
                forSaleProps = await uplandApiManager.GetForSalePropsByCityId(cityId);
                properties = localDataManager.GetPropertiesByCityId(cityId).ToDictionary(p => p.Id, p => p);
                forSaleProps = forSaleProps.Where(p => properties.ContainsKey(p.Prop_Id) && properties[p.Prop_Id].NeighborhoodId == Id).ToList();
            }
            else if (type.ToUpper() == "STREET")
            {
                List<Street> streets = localDataManager.GetStreets();

                if (!streets.Any(s => s.Id == Id))
                {
                    // Street don't exist
                    output.Add(string.Format("{0} is not a valid streetId. Try running my !SearchStreets command.", Id.ToString()));
                    return output;
                }

                cityId = streets.Where(n => n.Id == Id).First().CityId;
                forSaleProps = await uplandApiManager.GetForSalePropsByCityId(cityId);
                properties = localDataManager.GetPropertiesByCityId(cityId).ToDictionary(p => p.Id, p => p);
                forSaleProps = forSaleProps.Where(p => properties.ContainsKey(p.Prop_Id) && properties[p.Prop_Id].StreetId == Id).ToList();
            }
            else if(type.ToUpper() == "COLLECTION")
            {
                List<Collection> collections = localDataManager.GetCollections();

                if (!collections.Any(c => c.Id == Id))
                {
                    // Collection don't exist
                    output.Add(string.Format("{0} is not a valid collectionId. Try running my !CollectionInfo command.", Id.ToString()));
                    return output;
                }

                if (collections.Any(c => c.Id == Id && c.IsCityCollection))
                {
                    // Don't do city collections
                    output.Add(string.Format("This doesn't work for city collections.", Id.ToString()));
                    return output;
                }

                Collection collection = collections.Where(c => c.Id == Id).First();
                cityId = collection.CityId.Value;
                forSaleProps = await uplandApiManager.GetForSalePropsByCityId(cityId);
                properties = localDataManager.GetPropertiesByCityId(cityId).ToDictionary(p => p.Id, p => p);
                forSaleProps = forSaleProps.Where(p => collection.MatchingPropertyIds.Contains(p.Prop_Id)).ToList();
            }
            else
            {
                output.Add(string.Format("That wasn't a valid type. Choose: City, Neighborhood, or Collection"));
                return output;
            }

            if (currency == "USD")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "USD").ToList();
            }
            else if (currency == "UPX")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "UPX").ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            forSaleProps = forSaleProps.Where(p => propertyStructures.ContainsKey(p.Prop_Id)).ToList();

            if (forSaleProps.Count == 0)
            {
                // Nothing on sale
                output.Add(string.Format("There are no buildings on sale for {0} in {1} {2}", currency, type.ToUpper(), Id));
                return output;
            }

            if (orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].MonthlyEarnings * 12 / 0.1728).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, properties, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report Buildings In {0}Id {1}.", type.ToUpper(), Id), uplandApiManager.GetCacheDateTime(cityId)));
            }

            return output;
        }

        public async Task<List<string>> GetCityPropertiesForSale(int cityId, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            if (!Consts.Cities.ContainsKey(cityId))
            {
                output.Add(string.Format("{0} is not a valid cityId.", cityId));
                return output;
            }

            Dictionary<long, Property> propDictionary = new Dictionary<long, Property>();
            List<UplandForSaleProp> forSaleProps = new List<UplandForSaleProp>();

            forSaleProps.AddRange(await uplandApiManager.GetForSalePropsByCityId(cityId));
            List<Property> allProperties = new List<Property>();

            allProperties.AddRange(localDataManager.GetPropertiesByCityId(cityId));
                
            foreach (Property prop in allProperties)
            {
                if (!propDictionary.ContainsKey(prop.Id))
                {
                    propDictionary.Add(prop.Id, prop);
                }
            }

            forSaleProps = forSaleProps.Where(p => propDictionary.ContainsKey(p.Prop_Id)).ToList();

            if (currency == "USD")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "USD").ToList();
            }
            else if (currency == "UPX")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "UPX").ToList();
            }

            if (forSaleProps.Count == 0)
            {
                output.Add(string.Format("No Props Found, I bet you tried to run this on Piedmont or something."));
                return output;
            }

            if (orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / propDictionary[p.Prop_Id].MonthlyEarnings * 12 / 0.1728).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, propDictionary, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, propDictionary, propertyStructures, string.Format("For Sale Report For CityId {0}.", cityId), uplandApiManager.GetCacheDateTime(cityId)));
            }

            return output;
        }

        public async Task<List<string>> GetStreetPropertiesForSale(int streetId, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            List<Street> streets = localDataManager.GetStreets();

            if (!streets.Any(s => s.Id == streetId))
            {
                // Neighborhood don't exist
                output.Add(string.Format("{0} is not a valid streetId. Try running my !StreetInfo or !SearchStreets command.", streetId.ToString()));
                return output;
            }

            Street street = streets.Where(s => s.Id == streetId).First();
            List<UplandForSaleProp> forSaleProps = await uplandApiManager.GetForSalePropsByCityId(street.CityId);

            if (currency == "USD")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "USD").ToList();
            }
            else if (currency == "UPX")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "UPX").ToList();
            }

            Dictionary<long, Property> properties = localDataManager.GetPropertiesByCityId(street.CityId).ToDictionary(p => p.Id, p => p);

            forSaleProps = forSaleProps.Where(p => properties.ContainsKey(p.Prop_Id) && properties[p.Prop_Id].StreetId == streetId).ToList();

            if (forSaleProps.Count == 0)
            {
                // Nothing on sale
                output.Add(string.Format("There is nothing on sale on this street.", streetId.ToString()));
                return output;
            }

            if (orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].MonthlyEarnings * 12 / 0.1728).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, properties, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report for {0}", street.Name), uplandApiManager.GetCacheDateTime(street.CityId)));
            }

            return output;
        }

        public List<string> GetUnmintedProperties(string type, int Id, string propType, string fileType)
        {
            List<string> output = new List<string>();
            Dictionary<long, Property> properties = new Dictionary<long, Property>();
            int cityId = 0;

            if (type.ToUpper() == "CITY")
            {
                cityId = Id;
                if (!Consts.Cities.ContainsKey(Id))
                {
                    output.Add(string.Format("{0} is not a valid cityId. Try running my !CityInfo command.", Id));
                    return output;
                }
                properties = localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => p.Status != "Locked" && p.Status != "Owned" && p.Status != "For sale")
                    .OrderBy(p => p.MonthlyEarnings)
                    .ToDictionary(p => p.Id, p => p);
            }
            else if (type.ToUpper() == "NEIGHBORHOOD")
            {
                List<Neighborhood> neighborhoods = localDataManager.GetNeighborhoods();

                if (!neighborhoods.Any(n => n.Id == Id))
                {
                    // Neighborhood don't exist
                    output.Add(string.Format("{0} is not a valid neighborhoodId. Try running my !NeighborhoodInfo command.", Id.ToString()));
                    return output;
                }

                cityId = neighborhoods.Where(n => n.Id == Id).First().CityId;
                properties = localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => p.NeighborhoodId == Id)
                    .Where(p => p.Status != "Locked" && p.Status != "Owned" && p.Status != "For sale")
                    .OrderBy(p => p.MonthlyEarnings)
                    .ToDictionary(p => p.Id, p => p);
            }
            else if (type.ToUpper() == "COLLECTION")
            {
                List<Collection> collections = localDataManager.GetCollections();

                if (!collections.Any(c => c.Id == Id))
                {
                    // Collection don't exist
                    output.Add(string.Format("{0} is not a valid collectionId. Try running my !CollectionInfo command.", Id.ToString()));
                    return output;
                }

                if (collections.Any(c => c.Id == Id && c.IsCityCollection))
                {
                    // Don't do city collections
                    output.Add(string.Format("This doesn't work for city collections.", Id.ToString()));
                    return output;
                }

                Collection collection = collections.Where(c => c.Id == Id).First();
                cityId = collection.CityId.Value;
                properties = localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => collection.MatchingPropertyIds.Contains(p.Id))
                    .Where(p => p.Status != "Locked" && p.Status != "Owned" && p.Status != "For sale")
                    .OrderBy(p => p.MonthlyEarnings)
                    .ToDictionary(p => p.Id, p => p);
            }
            else if (type.ToUpper() == "STREET")
            {
                List<Street> streets = localDataManager.GetStreets();

                if (!streets.Any(s => s.Id == Id))
                {
                    // Street don't exist
                    output.Add(string.Format("{0} is not a valid streetId. Try running my !SearchStreets command.", Id.ToString()));
                    return output;
                }

                cityId = streets.Where(n => n.Id == Id).First().CityId;
                properties = localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => p.StreetId == Id)
                    .Where(p => p.Status != "Locked" && p.Status != "Owned" && p.Status != "For sale")
                    .OrderBy(p => p.MonthlyEarnings)
                    .ToDictionary(p => p.Id, p => p);
            }
            else
            {
                output.Add(string.Format("That wasn't a valid type. Choose: City, Neighborhood, or Collection"));
                return output;
            }

            if (propType == "FSA")
            {
                properties = properties.Where(p => p.Value.FSA).ToDictionary(p => p.Key, p => p.Value);
            }
            else if (propType == "NONFSA")
            {
                properties = properties.Where(p => !p.Value.FSA).ToDictionary(p => p.Key, p => p.Value);
            }

            if (properties.Count == 0)
            {
                // Nothing unminted in range
                output.Add(string.Format("There are no uniminted properties for {0} {1}", type, Id));
                return output;
            }

            if (fileType == "CSV")
            {
                output.Add("PropertyId,Size,Mint,NeighborhoodId,CityId,Address");

                foreach (Property property in properties.Values)
                {
                    string propString = "";

                    propString += string.Format("{0},", property.Id);
                    propString += string.Format("{0},", property.Size);
                    propString += string.Format("{0:F0},", Math.Round(property.MonthlyEarnings * 12 / 0.1728));
                    propString += string.Format("{0},", property.NeighborhoodId.HasValue ? property.NeighborhoodId.Value.ToString() : "-1");
                    propString += string.Format("{0},", property.CityId);
                    propString += string.Format("{0},", property.Address);
                    output.Add(propString);
                }
            }
            else
            {
                int idPad = 19;
                int sizePad = 7;
                int mintPad = 13;
                int neighborhoodPad = 14;
                int cityPad = 6;
                int addressPad = properties.Max(p => p.Value.Address.Length);

                output.Add(string.Format("{0} Unminted Properties For {1} {2}", propType, type, Id));
                output.Add("");
                output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5}"
                    , "Id".PadLeft(idPad)
                    , "Size".PadLeft(sizePad)
                    , "Mint".PadLeft(mintPad)
                    , "NeighborhoodId".PadLeft(neighborhoodPad)
                    , "CityId".PadLeft(cityPad)
                    , "Address".PadLeft(addressPad)));

                foreach (Property property in properties.Values)
                {
                    output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5}"
                        , property.Id.ToString().PadLeft(idPad)
                        , string.Format("{0:N0}", property.Size).PadLeft(sizePad)
                        , string.Format("{0:N2}", Math.Round(property.MonthlyEarnings * 12 / 0.1728)).ToString().PadLeft(mintPad)
                        , string.Format("{0}", property.NeighborhoodId.HasValue ? property.NeighborhoodId.Value.ToString() : "-1").PadLeft(neighborhoodPad)
                        , string.Format("{0}", cityId).PadLeft(cityPad)
                        , property.Address.PadLeft(addressPad)
                    ));
                }
            }

            return output;
        }

        public List<string> GetAllProperties(string type, int Id, string fileType)
        {
            List<string> output = new List<string>();
            Dictionary<long, Property> properties = new Dictionary<long, Property>();
            int cityId = 0;

            if (type.ToUpper() == "CITY")
            {
                cityId = Id;
                if (!Consts.Cities.ContainsKey(Id))
                {
                    output.Add(string.Format("{0} is not a valid cityId. Try running my !CityInfo command.", Id));
                    return output;
                }

                if (cityId != 12 && cityId != 13)
                {
                    output.Add(string.Format("I can't run this command on City level data. There's just too much data!", Id.ToString()));
                    return output;
                }

                properties = localDataManager
                    .GetPropertiesByCityId(cityId)
                    .OrderBy(p => p.MonthlyEarnings)
                    .ToDictionary(p => p.Id, p => p);
            }
            else if (type.ToUpper() == "NEIGHBORHOOD")
            {
                List<Neighborhood> neighborhoods = localDataManager.GetNeighborhoods();

                if (!neighborhoods.Any(n => n.Id == Id))
                {
                    // Neighborhood don't exist
                    output.Add(string.Format("{0} is not a valid neighborhoodId. Try running my !NeighborhoodInfo command.", Id.ToString()));
                    return output;
                }

                cityId = neighborhoods.Where(n => n.Id == Id).First().CityId;
                properties = localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => p.NeighborhoodId == Id)
                    .OrderBy(p => p.MonthlyEarnings)
                    .ToDictionary(p => p.Id, p => p);
            }
            else if (type.ToUpper() == "STREET")
            {
                List<Street> streets = localDataManager.GetStreets();

                if (!streets.Any(s => s.Id == Id))
                {
                    // Street don't exist
                    output.Add(string.Format("{0} is not a valid streetId. Try running my !SearchStreets command.", Id.ToString()));
                    return output;
                }

                cityId = streets.Where(n => n.Id == Id).First().CityId;
                properties = localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => p.StreetId == Id)
                    .OrderBy(p => p.MonthlyEarnings)
                    .ToDictionary(p => p.Id, p => p);
            }
            else if (type.ToUpper() == "COLLECTION")
            {
                List<Collection> collections = localDataManager.GetCollections();

                if (!collections.Any(c => c.Id == Id))
                {
                    // Collection don't exist
                    output.Add(string.Format("{0} is not a valid collectionId. Try running my !CollectionInfo command.", Id.ToString()));
                    return output;
                }

                if (collections.Any(c => c.Id == Id && c.IsCityCollection))
                {
                    // Don't do city collections
                    output.Add(string.Format("This doesn't work for city collections.", Id.ToString()));
                    return output;
                }

                Collection collection = collections.Where(c => c.Id == Id).First();
                cityId = collection.CityId.Value;
                properties = localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => collection.MatchingPropertyIds.Contains(p.Id))
                    .OrderBy(p => p.MonthlyEarnings)
                    .ToDictionary(p => p.Id, p => p);
            }
            else
            {
                output.Add(string.Format("That wasn't a valid type. Choose: City, Neighborhood, or Collection"));
                return output;
            }

            if (properties.Count == 0)
            {
                // Nothing in range
                output.Add(string.Format("There are no properties, somehow, for {0} {1}", type, Id));
                return output;
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.Add("PropertyId,Size,Mint,NeighborhoodId,CityId,Status,FSA,Address,Structure");

                foreach (Property property in properties.Values)
                {
                    string propString = "";

                    propString += string.Format("{0},", property.Id);
                    propString += string.Format("{0},", property.Size);
                    propString += string.Format("{0:F0},", Math.Round(property.MonthlyEarnings * 12 / 0.1728));
                    propString += string.Format("{0},", property.NeighborhoodId.HasValue ? property.NeighborhoodId.Value.ToString() : "-1");
                    propString += string.Format("{0},", property.CityId);
                    propString += string.Format("{0},", property.Status);
                    propString += string.Format("{0},", property.FSA);
                    propString += string.Format("{0},", property.Address);
                    propString += string.Format("{0}", propertyStructures.ContainsKey(property.Id) ? propertyStructures[property.Id] : "None");
                    output.Add(propString);
                }
            }
            else
            {
                int idPad = 19;
                int sizePad = 7;
                int mintPad = 13;
                int neighborhoodPad = 14;
                int cityPad = 6;
                int statusPad = 8;
                int fsaPad = 5;
                int addressPad = properties.Max(p => p.Value.Address.Length);
                int structurePad = propertyStructures.Max(p => p.Value.Length);

                output.Add(string.Format("Properties For {0} {1}", type, Id));
                output.Add("");
                output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6} - {7} - {8}"
                    , "Id".PadLeft(idPad)
                    , "Size".PadLeft(sizePad)
                    , "Mint".PadLeft(mintPad)
                    , "NeighborhoodId".PadLeft(neighborhoodPad)
                    , "CityId".PadLeft(cityPad)
                    , "Status".PadLeft(statusPad)
                    , "FSA".PadLeft(fsaPad)
                    , "Address".PadLeft(addressPad)
                    , "Structure".PadLeft(structurePad)));

                foreach (Property property in properties.Values)
                {
                    output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6} - {7} - {8}"
                        , property.Id.ToString().PadLeft(idPad)
                        , string.Format("{0:N0}", property.Size).PadLeft(sizePad)
                        , string.Format("{0:N2}", Math.Round(property.MonthlyEarnings * 12 / 0.1728)).ToString().PadLeft(mintPad)
                        , string.Format("{0}", property.NeighborhoodId.HasValue ? property.NeighborhoodId.Value.ToString() : "-1").PadLeft(neighborhoodPad)
                        , string.Format("{0}", cityId).PadLeft(cityPad)
                        , string.Format("{0}", property.Status).PadLeft(statusPad)
                        , string.Format("{0}", property.FSA).PadLeft(fsaPad)
                        , property.Address.PadLeft(addressPad)
                        , string.Format("{0}", propertyStructures.ContainsKey(property.Id) ? propertyStructures[property.Id] : "None").PadLeft(structurePad)
                    ));
                }
            }

            return output;
        }

        public async Task<List<string>> GetAssetsByTypeAndUserName(string type, string userName, string fileType)
        {
            List<string> output = new List<string>(); 
            List<Asset> assets = new List<Asset>();

            type = type.ToUpper();
            userName = userName.ToLower();

            if (type == "NFLPA")
            {
                assets.AddRange(await uplandApiManager.GetNFLPALegitsByUsername(userName));
            }
            else if(type == "SPIRIT")
            {
                assets.AddRange(await uplandApiManager.GetSpiritLegitsByUsername(userName));
            }
            else if (type == "DECORATION")
            {
                assets.AddRange(await uplandApiManager.GetDecorationsByUsername(userName));
            }
            else
            {
                output.Add(string.Format("That wasn't a valid type. Choose: NFLPA, Spirit, or Decoration"));
                return output;
            }

            if (assets.Count == 0)
            {
                // Nothing found
                output.Add(string.Format("No {0} assets found for {1}.", type, userName));
                return output;
            }

            switch (type)
            {
                case "NFLPA":
                    assets = assets.OrderBy(a => ((NFLPALegit)a).PlayerName).OrderBy(a => ((NFLPALegit)a).LegitType).OrderBy(a => ((NFLPALegit)a).TeamName).ToList();
                    break;
                case "SPIRIT":
                    assets = assets.OrderBy(a => a.DisplayName).OrderBy(a => ((SpiritLegit)a).Rarity).ToList();
                    break;
                case "DECORATION":
                    assets = assets.OrderBy(a => a.DisplayName).OrderBy(a => ((Decoration)a).Rarity).ToList();
                    break;
            }

            if (fileType == "CSV")
            {
                switch (type)
                {
                    case "NFLPA":
                        output.Add("Team,Player,Type,Year,Mint,Current Supply,Max Supply,Link");
                        break;
                    case "SPIRIT":
                        output.Add("Name,Rarity,Mint,Current Supply,Max Supply,Link");
                        break;
                    case "DECORATION":
                        output.Add("Name,Building,Rarity,Mint,Current Supply,Max Supply,Link");
                        break;
                }

                foreach (Asset asset in assets)
                {
                    string assetString = "";

                    switch (type)
                    {
                        case "NFLPA":
                            assetString += string.Format("{0},", ((NFLPALegit)asset).TeamName);
                            assetString += string.Format("{0},", ((NFLPALegit)asset).PlayerName);
                            assetString += string.Format("{0},", ((NFLPALegit)asset).LegitType);
                            assetString += string.Format("{0},", ((NFLPALegit)asset).Year);
                            break;
                        case "SPIRIT":
                            assetString += string.Format("{0},", asset.DisplayName);
                            assetString += string.Format("{0},", ((SpiritLegit)asset).Rarity);
                            break;
                        case "DECORATION":
                            assetString += string.Format("{0},", asset.DisplayName);
                            assetString += string.Format("{0},", ((Decoration)asset).Subtitle);
                            assetString += string.Format("{0},", ((Decoration)asset).Rarity);
                            break;
                    }

                    assetString += string.Format("{0},", asset.Mint);
                    assetString += string.Format("{0},", asset.CurrentSupply);
                    assetString += string.Format("{0},", asset.MaxSupply);
                    assetString += string.Format("{0}", asset.Link);

                    output.Add(assetString);
                }
            }
            else
            {
                int slotOnePad = 0;
                int slotTwoPad = 0;
                int slotThreePad = 0;
                int slotFourPad = 0;
                int mintPad = 6;
                int currentPad = 14;
                int maxPad = 10;
                int linkPad = assets.Max(a => a.Link.Length);

                output.Add(string.Format("{0} Assets Owned By {1}", type, userName));
                output.Add("");

                switch (type)
                {
                    case "NFLPA":
                        slotOnePad = assets.Max(a => ((NFLPALegit)a).TeamName.Length);
                        slotTwoPad = assets.Max(a => ((NFLPALegit)a).PlayerName.Length);
                        slotThreePad = assets.Max(a => ((NFLPALegit)a).LegitType.Length);
                        slotFourPad = 4;

                        output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6} - {7}"
                            , "Team".PadLeft(slotOnePad)
                            , "Player".PadLeft(slotTwoPad)
                            , "Type".PadLeft(slotThreePad)
                            , "Year".PadLeft(slotFourPad)
                            , "Mint".PadLeft(mintPad)
                            , "Current Supply".PadLeft(currentPad)
                            , "Max Supply".PadLeft(maxPad)
                            , "Link".PadLeft(linkPad)));
                        break;
                    case "SPIRIT":
                        slotOnePad = assets.Max(a => a.DisplayName.Length);
                        slotTwoPad = assets.Max(a => ((SpiritLegit)a).DisplayName.Length);

                        output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5}"
                            , "Name".PadLeft(slotOnePad)
                            , "Rarity".PadLeft(slotTwoPad)
                            , "Mint".PadLeft(mintPad)
                            , "Current Supply".PadLeft(currentPad)
                            , "Max Supply".PadLeft(maxPad)
                            , "Link".PadLeft(linkPad)));
                        break;
                    case "DECORATION":
                        slotOnePad = assets.Max(a => a.DisplayName.Length);
                        slotTwoPad = assets.Max(a => ((Decoration)a).Subtitle.Length);
                        slotThreePad = assets.Max(a => ((Decoration)a).Rarity.Length);

                        output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6}"
                            , "Name".PadLeft(slotOnePad)
                            , "Building".PadLeft(slotTwoPad)
                            , "Rarity".PadLeft(slotThreePad)
                            , "Mint".PadLeft(mintPad)
                            , "Current Supply".PadLeft(currentPad)
                            , "Max Supply".PadLeft(maxPad)
                            , "Link".PadLeft(linkPad)));
                        break;
                }

                foreach (Asset asset in assets)
                {
                    switch (type)
                    {
                        case "NFLPA":
                            output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6} - {7}"
                                , ((NFLPALegit)asset).TeamName.PadLeft(slotOnePad)
                                , ((NFLPALegit)asset).PlayerName.PadLeft(slotTwoPad)
                                , ((NFLPALegit)asset).LegitType.PadLeft(slotThreePad)
                                , ((NFLPALegit)asset).Year.PadLeft(slotFourPad)
                                , string.Format("{0:N0}", asset.Mint).PadLeft(mintPad)
                                , string.Format("{0:N0}", asset.CurrentSupply).PadLeft(currentPad)
                                , string.Format("{0:N0}", asset.MaxSupply).PadLeft(maxPad)
                                , asset.Link.PadLeft(linkPad)));
                            break;
                        case "SPIRIT":
                            output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5}"
                                , asset.DisplayName.PadLeft(slotOnePad)
                                , ((SpiritLegit)asset).Rarity.PadLeft(slotTwoPad)
                                , string.Format("{0:N0}", asset.Mint).PadLeft(mintPad)
                                , string.Format("{0:N0}", asset.CurrentSupply).PadLeft(currentPad)
                                , string.Format("{0:N0}", asset.MaxSupply).PadLeft(maxPad)
                                , asset.Link.PadLeft(linkPad)));
                            break;
                        case "DECORATION":
                            output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6}"
                                , asset.DisplayName.PadLeft(slotOnePad)
                                , ((Decoration)asset).Subtitle.PadLeft(slotTwoPad)
                                , ((Decoration)asset).Rarity.PadLeft(slotThreePad)
                                , string.Format("{0:N0}", asset.Mint).PadLeft(mintPad)
                                , string.Format("{0:N0}", asset.CurrentSupply).PadLeft(currentPad)
                                , string.Format("{0:N0}", asset.MaxSupply).PadLeft(maxPad)
                                , asset.Link.PadLeft(linkPad)));
                            break;
                    }
                }
            }

            return output;
        }

        public void ClearSalesCache()
        {
            uplandApiManager.ClearSalesCache();
        }

        public async Task RebuildPropertyStructures()
        {
            List<PropertyStructure> propertyStructures = await blockchainManager.GetPropertyStructures();
            List<Neighborhood> neighborhoods = localDataManager.GetNeighborhoods();

            localDataManager.TruncatePropertyStructure();

            List<long> savedIds = new List<long>();

            foreach(PropertyStructure propertyStructure in propertyStructures)
            {
                if (!savedIds.Contains(propertyStructure.PropertyId))
                {
                    try
                    {
                        propertyStructure.StructureType = HelperFunctions.TranslateStructureType(propertyStructure.StructureType);
                        localDataManager.CreatePropertyStructure(propertyStructure);
                    }
                    catch
                    {
                        // Most likely fails due to a missing property
                        await localDataManager.PopulateIndividualPropertyById(propertyStructure.PropertyId, neighborhoods);
                        localDataManager.CreatePropertyStructure(propertyStructure);
                    }

                    savedIds.Add(propertyStructure.PropertyId);
                }
            }
        }

        public async Task RunCityStatusUpdate(bool allCities)
        {
            List<CollatedStatsObject> cityStats = localDataManager.GetCityStats();

            foreach (int cityId in Consts.Cities.Keys)
            {
                // Don't process the bullshit cities
                if(cityId >= 10000)
                {
                    continue;
                }

                // Skip sold out if allCities is not true
                if(!allCities)
                {
                    if (cityStats.Where(c => c.Id == cityId).First().PercentMinted >= 100)
                    {
                        continue;
                    }
                }
                
                List<double> cityCoordinates = HelperFunctions.GetCityAreaCoordinates(cityId);
                await localDataManager.PopulateAllPropertiesInArea(cityCoordinates[0], cityCoordinates[1], cityCoordinates[2], cityCoordinates[3], cityId, false);
                localDataManager.DetermineNeighborhoodIdsForCity(cityId);
            }
        }
    }
}
