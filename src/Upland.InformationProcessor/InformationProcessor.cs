using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public InformationProcessor()
        {
            localDataManager = new LocalDataManager();
            uplandApiManager = new UplandApiManager();
        }

        public List<string> GetCollectionInformation()
        {
            List<string> output = new List<string>();

            List<Collection> collections = localDataManager.GetCollections();

            collections = collections.OrderBy(c => c.Category).OrderBy(c => c.CityId).ToList();

            output.Add("      Id - Category   - Name - Boost - Slots - Reward - Number of Properties");
            output.Add("");
            output.Add("Standard Collections");

            int maxNameLenght = collections.OrderByDescending(c => c.Name.Length).First().Name.Length;

            int? cityId = -1;
            foreach (Collection collection in collections)
            {
                if (cityId != collection.CityId)
                {
                    cityId = collection.CityId;
                    output.Add("");
                    output.Add(Consts.Cities[cityId.Value]);
                }

                output.Add(string.Format("     {0} - {1} - {2} - {3:N2} - {4} - {5} - {6}"
                    , collection.Id.ToString().PadLeft(3)
                    , HelperFunctions.GetCollectionCategory(collection.Category).PadRight(10)
                    , collection.Name.PadRight(maxNameLenght)
                    , collection.Boost
                    , collection.NumberOfProperties
                    , string.Format("{0:N0}", collection.Reward).ToString().PadLeft(7)
                    , string.Format("{0:N0}", collection.MatchingPropertyIds.Count).ToString().PadLeft(6)
                ));
            }

            return output;
        }

        public List<string> GetNeighborhoodInformation()
        {
            List<string> output = new List<string>();

            List<Neighborhood> neighborhoods = localDataManager.GetNeighborhoods();

            int maxNameLength = neighborhoods.OrderByDescending(n => n.Name.Length).First().Name.Length;
            neighborhoods = neighborhoods.OrderBy(n => n.Name).OrderBy(n => n.CityId).ToList();

            output.Add(string.Format("       Id - {0}", "Name".PadLeft(maxNameLength)));
            output.Add("");

            int? cityId = -1;
            foreach (Neighborhood neighborhood in neighborhoods)
            {
                if (cityId != neighborhood.CityId)
                {
                    cityId = neighborhood.CityId;
                    output.Add("");
                    output.Add(Consts.Cities[cityId.Value]);
                }

                output.Add(string.Format("     {0} - {1}"
                    , neighborhood.Id.ToString().PadLeft(4)
                    , neighborhood.Name.PadLeft(maxNameLength)
                ));
            }

            return output;
        }

        public async Task<List<string>> GetMyPropertyInfo(string username)
        {
            List<string> output = new List<string>();
            List<Property> properties = await localDataManager.GetPropertysByUsername(username);

            properties = properties.OrderBy(p => p.Address).OrderBy(p => p.CityId).ToList();

            output.Add("                 Id -   Size - Monthly Earnings - NeighborhoodId - Address");

            int? cityId = -1;
            foreach (Property property in properties)
            {
                if (cityId != property.CityId)
                {
                    cityId = property.CityId;
                    output.Add("");
                    output.Add(Consts.Cities[cityId.Value]);
                }

                output.Add(string.Format("     {0} - {1} - {2:} - {3} - {4}"
                    , property.Id
                    , string.Format("{0:N0}", property.Size).ToString().PadLeft(6)
                    , string.Format("{0:N2}", property.MonthlyEarnings).ToString().PadLeft(10)
                    , string.Format("{0}", property.NeighborhoodId.HasValue ? property.NeighborhoodId.Value.ToString().PadLeft(4) : "-1")
                    , property.Address
                ));
            }

            return output;
        }

        public async Task<List<string>> GetCollectionPropertiesForSale(int collectionId, string orderBy, string currency)
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

            if(orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].MonthlyEarnings * 12/0.1728).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, collection.Name, Consts.Cities[collection.CityId.Value], uplandApiManager.GetCacheDateTime(collection.CityId.Value)));

            return output;
        }

        public async Task<List<string>> GetNeighborhoodPropertiesForSale(int neighborhoodId, string orderBy, string currency)
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

            output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, neighborhood.Name, Consts.Cities[neighborhood.CityId], uplandApiManager.GetCacheDateTime(neighborhood.CityId)));

            return output;
        }

        public List<string> GetCityIds()
        {
            List<string> array = new List<string>();
            array.Add("Id - Name");
            array.AddRange(Consts.Cities.Select(c => string.Format("{0} - {1}", c.Key.ToString().PadLeft(2), c.Value)).ToList());
            return array;
        }

        public async Task<List<string>> GetSalesDataByCityId(int cityId)
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
            

            if (forSaleProps.Count == 0)
            {
                output.Add(string.Format("No Props Found, I bet you tried to run this on Piedmont or something."));
                return output;
            }

            Dictionary<long, UplandForSaleProp> forSaleDictionary = new Dictionary<long, UplandForSaleProp>();
            foreach (UplandForSaleProp prop in forSaleProps)
            {
                if (!forSaleDictionary.ContainsKey(prop.Prop_Id))
                {
                    forSaleDictionary.Add(prop.Prop_Id, prop);
                }
            }

            output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleDictionary, propDictionary));

            return output;
        }

        public void ClearSalesCache()
        {
            uplandApiManager.ClearSalesCache();
        }
    }
}
