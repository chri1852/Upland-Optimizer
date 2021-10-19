using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Upland.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public static class HelperFunctions
    {
        public static string GetCollectionCategory(int category)
        {
            switch(category)
            {
                case 1:  { return "Standard";   }
                case 2:  { return "Limited";    }
                case 3:  { return "Exclusive";  }
                case 4:  { return "Rare";       }
                case 5:  { return "Ultra Rare"; }
                default: { return "Unknown";    }
            }
        }

        public static List<string> CreateForSaleCSVString(Dictionary<long, UplandForSaleProp> forSaleDictionary, Dictionary<long, Property> propDictionary)
        {
            List<string> output = new List<string>();
            output.Add("PropertyId,Price,Currency,Mint,Markup,CityId,Address,Owner,NeighborhoodId");

            foreach (UplandForSaleProp prop in forSaleDictionary.Values)
            {
                string propString = "";

                propString += string.Format("{0},", prop.Prop_Id);

                if (prop.Currency == "USD")
                {
                    propString += string.Format("{0:F2},", prop.Price);
                }
                else
                {
                    propString += string.Format("{0:F0},", prop.Price);
                }

                propString += string.Format("{0},", prop.Currency.ToUpper());
                propString += string.Format("{0:F0},", Math.Round(propDictionary[prop.Prop_Id].MonthlyEarnings * 12 / 0.1728));
                propString += string.Format("{0:F0}%,", 100 * prop.SortValue / (propDictionary[prop.Prop_Id].MonthlyEarnings * 12 / 0.1728));
                propString += string.Format("{0},", propDictionary[prop.Prop_Id].CityId);
                propString += string.Format("{0},", propDictionary[prop.Prop_Id].Address);
                propString += string.Format("{0},", prop.Owner);
                propString += string.Format("{0}", propDictionary[prop.Prop_Id].NeighborhoodId.HasValue ? propDictionary[prop.Prop_Id].NeighborhoodId.Value.ToString() : "-1");

                output.Add(propString);
            }

            return output;
        }

        public static List<string> ForSaleTxtString(List<UplandForSaleProp> forSaleProps, Dictionary<long, Property> properties, string reportName, string cityName, string expireDate)
        {
            List<string> output = new List<string>();

            int pricePad = 11;
            int markupPad = 9;
            int mintPad = 10;
            int addressPad = properties.Max(p => p.Value.Address.Length);
            int ownerPad = forSaleProps.Max(p => p.Owner.Length);
            output.Add(string.Format("For Sale Report for {0} in {1}. Data expires at {2}", reportName, cityName, expireDate));
            output.Add("");
            output.Add(string.Format("{0} - Currency - {1} - {2} - {3} - {4} - {5}", "Price".PadLeft(pricePad), "Mint".PadLeft(mintPad), "Markup".PadLeft(markupPad), "Address".PadLeft(addressPad), "Owner".PadLeft(ownerPad), "NeighborhoodId"));

            foreach (UplandForSaleProp prop in forSaleProps)
            {
                string propString = "";

                if (prop.Currency == "USD")
                {
                    propString += string.Format("{0:N2}", prop.Price).PadLeft(pricePad);
                }
                else
                {
                    propString += string.Format("{0:N0}", prop.Price).PadLeft(pricePad);
                }

                propString += string.Format(" -    {0}   - ", prop.Currency.ToUpper());
                propString += string.Format("{0:N0}", Math.Round(properties[prop.Prop_Id].MonthlyEarnings * 12 / 0.1728)).PadLeft(mintPad);
                propString += " - ";
                propString += string.Format("{0:N0}%", 100 * prop.SortValue / (properties[prop.Prop_Id].MonthlyEarnings * 12 / 0.1728)).PadLeft(markupPad);
                propString += " - ";
                propString += string.Format("{0}", properties[prop.Prop_Id].Address).PadLeft(addressPad);
                propString += " - ";
                propString += string.Format("{0}", prop.Owner).PadLeft(ownerPad);
                propString += " - ";
                propString += string.Format("{0}", properties[prop.Prop_Id].NeighborhoodId.HasValue ? properties[prop.Prop_Id].NeighborhoodId.Value.ToString().PadLeft(4) : "-1");

                output.Add(propString);
            }

            return output;
        }
    }
}
