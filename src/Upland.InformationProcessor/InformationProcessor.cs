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
                int propCountPad = 10;


                output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6}"
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

                    output.Add(string.Format("{0} - {1} - {2} - {3} - {4} - {5} - {6}"
                        , collection.Id.ToString().PadLeft(idPad)
                        , HelperFunctions.GetCollectionCategory(collection.Category).PadRight(categoryPad)
                        , collection.Name.PadLeft(NamePad)
                        , string.Format("{0:N2}", collection.Boost).PadLeft(boostPad)
                        , collection.NumberOfProperties.ToString().PadLeft(slotsPad)
                        , string.Format("{0:N0}", collection.Reward).ToString().PadLeft(rewardPad)
                        , string.Format("{0:N0}", collection.MatchingPropertyIds.Count).ToString().PadLeft(propCountPad)
                    ));
                }
            }
            else
            {
                output.Add("Id,Category,Name,Boost,Slots,Reward,NumberOfProperties");
                foreach (Collection collection in collections)
                {
                    output.Add(string.Format("{0},{1},{2},{3},{4},{5},{6}"
                        , collection.Id.ToString()
                        , HelperFunctions.GetCollectionCategory(collection.Category)
                        , collection.Name
                        , collection.Boost
                        , collection.NumberOfProperties
                        , string.Format("{0}", collection.Reward).ToString()
                        , string.Format("{0}", collection.MatchingPropertyIds.Count).ToString()
                    ));
                }
            }

            return output;
        }

        public List<string> GetNeighborhoodInformation(string fileType)
        {
            List<string> output = new List<string>();

            List<Neighborhood> neighborhoods = localDataManager.GetNeighborhoods();
            neighborhoods = neighborhoods.OrderBy(n => n.Name).OrderBy(n => n.CityId).ToList();

            if (fileType == "TXT")
            {
                int idPad = 9;
                int namePad = neighborhoods.OrderByDescending(n => n.Name.Length).First().Name.Length;

                output.Add(string.Format("{0} - {1}", "Id".PadLeft(idPad), "Name".PadLeft(namePad)));
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

                    output.Add(string.Format("{0} - {1}"
                        , neighborhood.Id.ToString().PadLeft(idPad)
                        , neighborhood.Name.PadLeft(namePad)
                    ));
                }
            }
            else
            {
                output.Add("Id,Name,CityId");
                foreach (Neighborhood neighborhood in neighborhoods)
                {
                    output.Add(string.Format("{0},{1},{2}"
                        , neighborhood.Id.ToString()
                        , neighborhood.Name.Replace(',', ' ')
                        , neighborhood.CityId
                    ));
                }
            }

            return output;
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
                    output.Add(string.Format("{0} is not a valid cityId.", Id));
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

        public List<string> GetCityIds(string fileType)
        {
            List<string> array = new List<string>();

            if (fileType == "TXT")
            {
                int namePad = Consts.Cities.OrderByDescending(c => c.Value.Length).First().Value.Length;

                array.Add(string.Format("Id - {0}", "Name".PadLeft(namePad)));
                array.AddRange(Consts.Cities.Select(c => string.Format("{0} - {1}", c.Key.ToString().PadLeft(5), c.Value.PadLeft(namePad))).ToList());
            }
            else
            {
                array.Add("Id,Name");
                array.AddRange(Consts.Cities.Select(c => string.Format("{0},{1}", c.Key.ToString(), c.Value)).ToList());
            }
            return array;
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

        public void ClearSalesCache()
        {
            uplandApiManager.ClearSalesCache();
        }

        public async Task RebuildPropertyStructures()
        {
            List<PropertyStructure> propertyStructures = await blockchainManager.GetPropertyStructures();

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
                        await localDataManager.PopulateIndividualPropertyById(propertyStructure.PropertyId);
                        localDataManager.CreatePropertyStructure(propertyStructure);
                    }

                    savedIds.Add(propertyStructure.PropertyId);
                }
            }
        }

        public async Task RunCityStatusUpdate()
        { 
            foreach (int cityId in Consts.Cities.Keys)
            {
                // Don't process the bullshit cities
                if(cityId >= 10000)
                {
                    continue;
                }

                List<double> cityCoordinates = HelperFunctions.GetCityAreaCoordinates(cityId);
                await localDataManager.PopulateAllPropertiesInArea(cityCoordinates[0], cityCoordinates[1], cityCoordinates[2], cityCoordinates[3], cityId, false);
                localDataManager.DetermineNeighborhoodIdsForCity(cityId);
            }
        }
    }
}
