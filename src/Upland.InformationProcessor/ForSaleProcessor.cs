using System;
using System.Collections.Generic;
using System.Linq;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public class ForSaleProcessor : IForSaleProcessor
    {
        private readonly ILocalDataManager _localDataManager;

        public ForSaleProcessor(ILocalDataManager localDataManager)
        {
            _localDataManager = localDataManager;
        }

        public List<string> GetCollectionPropertiesForSale(int collectionId, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            List<Collection> collections = _localDataManager.GetCollections();

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
            List<UplandForSaleProp> forSaleProps = _localDataManager.GetPropertiesForSale_Collection(collection.Id, false);

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
                // Nothing on sale
                output.Add(string.Format("There is nothing on sale in this collection.", collectionId.ToString()));
                return output;
            }

            Dictionary<long, Property> properties = _localDataManager.GetPropertiesByCollectionId(collectionId).ToDictionary(p => p.Id, p => p);

            if (orderBy == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].Mint).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, properties, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report for {0}", collection.Name), string.Format("{0:MM/dd/yyy HH:mm:ss}", DateTime.Now)));
            }

            return output.Take(Consts.MAX_LINES_TO_RETURN).ToList();
        }

        public List<string> GetNeighborhoodPropertiesForSale(int neighborhoodId, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            List<Neighborhood> neighborhoods = _localDataManager.GetNeighborhoods();

            if (!neighborhoods.Any(n => n.Id == neighborhoodId))
            {
                // Neighborhood don't exist
                output.Add(string.Format("{0} is not a valid neighborhoodId. Try running my !NeighborhoodInfo command.", neighborhoodId.ToString()));
                return output;
            }

            Neighborhood neighborhood = neighborhoods.Where(n => n.Id == neighborhoodId).First();
            List<UplandForSaleProp> forSaleProps = _localDataManager.GetPropertiesForSale_Neighborhood(neighborhood.Id, false);

            if (currency == "USD")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "USD").ToList();
            }
            else if (currency == "UPX")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "UPX").ToList();
            }

            Dictionary<long, Property> properties = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(f => f.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p); ;

            if (forSaleProps.Count == 0)
            {
                // Nothing on sale
                output.Add(string.Format("There is nothing on sale in this neighborhood.", neighborhoodId.ToString()));
                return output;
            }

            if (orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].Mint).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, properties, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report for {0}", neighborhood.Name), string.Format("{0:MM/dd/yyy HH:mm:ss}", DateTime.Now)));
            }

            return output.Take(Consts.MAX_LINES_TO_RETURN).ToList();
        }

        public List<string> GetBuildingPropertiesForSale(string type, int Id, string orderBy, string currency, string fileType)
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
                    foreach (int cId in Consts.Cities.Keys)
                    {
                        forSaleProps.AddRange(_localDataManager.GetPropertiesForSale_City(cId, true));
                    }

                    properties = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(g => g.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p);
                }
                else
                {
                    forSaleProps = _localDataManager.GetPropertiesForSale_City(cityId, true);
                    properties = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(g => g.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p);
                }

                cityId = 1; // Fix for Sales Cache time, just use San Francisco
            }
            else if (type.ToUpper() == "NEIGHBORHOOD")
            {
                List<Neighborhood> neighborhoods = _localDataManager.GetNeighborhoods();

                if (!neighborhoods.Any(n => n.Id == Id))
                {
                    // Neighborhood don't exist
                    output.Add(string.Format("{0} is not a valid neighborhoodId. Try running my !NeighborhoodInfo command.", Id.ToString()));
                    return output;
                }

                Neighborhood neighborhood = neighborhoods.Where(n => n.Id == Id).First();
                cityId = neighborhood.CityId;
                forSaleProps = _localDataManager.GetPropertiesForSale_Neighborhood(neighborhood.Id, true);
                properties = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(g => g.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p);
            }
            else if (type.ToUpper() == "STREET")
            {
                List<Street> streets = _localDataManager.GetStreets();

                if (!streets.Any(s => s.Id == Id))
                {
                    // Street don't exist
                    output.Add(string.Format("{0} is not a valid streetId. Try running my !SearchStreets command.", Id.ToString()));
                    return output;
                }

                forSaleProps = _localDataManager.GetPropertiesForSale_Street(Id, true);
                properties = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(g => g.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p);
            }
            else if (type.ToUpper() == "COLLECTION")
            {
                List<Collection> collections = _localDataManager.GetCollections();

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
                forSaleProps = _localDataManager.GetPropertiesForSale_Collection(collection.Id, true);
                properties = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(g => g.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p);
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
            Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            forSaleProps = forSaleProps.Where(p => propertyStructures.ContainsKey(p.Prop_Id)).ToList();

            if (forSaleProps.Count == 0)
            {
                // Nothing on sale
                output.Add(string.Format("There are no buildings on sale for {0} in {1} {2}", currency, type.ToUpper(), Id));
                return output;
            }

            if (orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].Mint).ToList();
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
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report Buildings In {0}Id {1}.", type.ToUpper(), Id), string.Format("{0:MM/dd/yyy HH:mm:ss}", DateTime.Now)));
            }

            return output.Take(Consts.MAX_LINES_TO_RETURN).ToList();
        }

        public List<string> GetCityPropertiesForSale(int cityId, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            if (!Consts.Cities.ContainsKey(cityId))
            {
                output.Add(string.Format("{0} is not a valid cityId.", cityId));
                return output;
            }

            Dictionary<long, Property> propDictionary = new Dictionary<long, Property>();
            List<UplandForSaleProp> forSaleProps = new List<UplandForSaleProp>();

            forSaleProps.AddRange(_localDataManager.GetPropertiesForSale_City(cityId, false));
            propDictionary = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(f => f.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p);

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
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / propDictionary[p.Prop_Id].Mint).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, propDictionary, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, propDictionary, propertyStructures, string.Format("For Sale Report For CityId {0}.", cityId), string.Format("{0:MM/dd/yyy HH:mm:ss}", DateTime.Now)));
            }

            return output.Take(Consts.MAX_LINES_TO_RETURN).ToList();
        }

        public List<string> GetStreetPropertiesForSale(int streetId, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            List<Street> streets = _localDataManager.GetStreets();

            if (!streets.Any(s => s.Id == streetId))
            {
                // Neighborhood don't exist
                output.Add(string.Format("{0} is not a valid streetId. Try running my !StreetInfo or !SearchStreets command.", streetId.ToString()));
                return output;
            }

            Street street = streets.Where(s => s.Id == streetId).First();
            List<UplandForSaleProp> forSaleProps = _localDataManager.GetPropertiesForSale_Street(street.Id, false);

            if (currency == "USD")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "USD").ToList();
            }
            else if (currency == "UPX")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "UPX").ToList();
            }

            Dictionary<long, Property> properties = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(f => f.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p);

            if (forSaleProps.Count == 0)
            {
                // Nothing on sale
                output.Add(string.Format("There is nothing on sale on this street.", streetId.ToString()));
                return output;
            }

            if (orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].Mint).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, properties, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report for {0}", street.Name), string.Format("{0:MM/dd/yyy HH:mm:ss}", DateTime.Now)));
            }

            return output.Take(Consts.MAX_LINES_TO_RETURN).ToList();
        }

        public List<string> GetUsernamePropertiesForSale(string uplandUsername, string orderBy, string currency, string fileType)
        {
            List<string> output = new List<string>();

            List<UplandForSaleProp> forSaleProps = _localDataManager.GetPropertiesForSale_Seller(uplandUsername, false);

            if (currency == "USD")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "USD").ToList();
            }
            else if (currency == "UPX")
            {
                forSaleProps = forSaleProps.Where(p => p.Currency == "UPX").ToList();
            }

            Dictionary<long, Property> properties = _localDataManager.GetProperties(forSaleProps.GroupBy(f => f.Prop_Id).Select(f => f.First().Prop_Id).ToList()).ToDictionary(p => p.Id, p => p);

            if (forSaleProps.Count == 0)
            {
                // Nothing on sale
                output.Add(string.Format("There is nothing for sale by {0}.", uplandUsername));
                return output;
            }

            if (orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].Mint).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Lets grab the Structures
            Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);

            if (fileType == "CSV")
            {
                output.AddRange(HelperFunctions.CreateForSaleCSVString(forSaleProps, properties, propertyStructures));
            }
            else
            {
                output.AddRange(HelperFunctions.ForSaleTxtString(forSaleProps, properties, propertyStructures, string.Format("For Sale Report for {0}", uplandUsername), string.Format("{0:MM/dd/yyy HH:mm:ss}", DateTime.Now)));
            }

            return output.Take(Consts.MAX_LINES_TO_RETURN).ToList();
        }
    }
}
