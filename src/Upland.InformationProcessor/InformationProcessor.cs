using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
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

        public async Task<List<string>> GetMyPropertyInfo(string username)
        {
            List<string> output = new List<string>();
            List<Property> properties = await localDataManager.GetPropertysByUsername(username);

            properties = properties.OrderBy(p => p.Address).OrderBy(p => p.CityId).ToList();

            output.Add("                 Id -   Size - Monthly Earnings - Address");

            int? cityId = -1;
            foreach (Property property in properties)
            {
                if (cityId != property.CityId)
                {
                    cityId = property.CityId;
                    output.Add("");
                    output.Add(Consts.Cities[cityId.Value]);
                }

                output.Add(string.Format("     {0} - {1} - {2:} - {3}"
                    , property.Id
                    , string.Format("{0:N0}", property.Size).ToString().PadLeft(6)
                    , string.Format("{0:N2}", property.MonthlyEarnings).ToString().PadLeft(10)
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

            Dictionary<long, Property> properties = localDataManager.GetPropertiesByCollectionId(collectionId)
                .Where(p => forSaleProps.Any(f => f.Prop_Id == p.Id)).ToDictionary(p => p.Id, p => p);

            if(orderBy.ToUpper() == "MARKUP")
            {
                forSaleProps = forSaleProps.OrderBy(p => 100 * p.SortValue / properties[p.Prop_Id].MonthlyEarnings * 12/0.1728).ToList();
            }
            else // PRICE
            {
                forSaleProps = forSaleProps.OrderBy(p => p.SortValue).ToList();
            }

            // Finally we are ready to write the output
            int pricePad = 10;
            int markupPad = 9;
            int mintPad = 10;
            int addressPad = properties.Max(p => p.Value.Address.Length);
            int ownerPad = forSaleProps.Max(p => p.Owner.Length);
            output.Add(string.Format("For Sale Report for {0} in {1}. Data expires at {2}", collection.Name, Consts.Cities[collection.CityId.Value], uplandApiManager.GetCacheDateTime(collection.CityId.Value)));
            output.Add("");
            output.Add(string.Format("{0} - Currency - {1} - {2} - {3} - {4}", "Price".PadLeft(pricePad), "Mint".PadLeft(mintPad), "Markup".PadLeft(markupPad), "Address".PadLeft(addressPad), "Owner".PadLeft(ownerPad)));
            
            foreach (UplandForSaleProp prop in forSaleProps)
            {
                string propString = "";

                if(prop.Currency == "USD")
                {
                    propString += string.Format("{0:N2}", prop.Price).PadLeft(pricePad);
                }
                else
                {
                    propString += string.Format("{0:N0}", prop.Price).PadLeft(pricePad);
                }

                propString += string.Format(" -    {0}   - ", prop.Currency.ToUpper());
                propString += string.Format("{0:N0}", Math.Round(properties[prop.Prop_Id].MonthlyEarnings*12/0.1728)).PadLeft(mintPad);
                propString += " - ";
                propString += string.Format("{0:N0}%", 100 * prop.SortValue/(properties[prop.Prop_Id].MonthlyEarnings * 12/0.1728)).PadLeft(markupPad);
                propString += " - ";
                propString += string.Format("{0}", properties[prop.Prop_Id].Address).PadLeft(addressPad);
                propString += " - ";
                propString += string.Format("{0}", prop.Owner).PadLeft(ownerPad);

                output.Add(propString);
            }

            return output;
        }
    }
}
