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

        public static string TranslateStructureType(string structureType)
        {
            switch (structureType)
            {
                case "smltownhouse": { return "Small Town House Pitched Roof"; }
                case "townhouse"   : { return "Town House"; }
                case "aptbuilding" : { return "Apartment Building"; }
                case "ranchhouse"  : { return "Ranch House"; }
                case "smltwnhouse2": { return "Small Town House Flat Roof"; }
                case "luxmodern"   : { return "Luxury Modern House"; }
                case "luxranch"    : { return "Luxury Ranch House"; }
                case "ferrybuildng": { return "San Francisco Ferry Building"; }
                case "coittower"   : { return "Coit Tower"; }
                case "chiftblstdm" : { return "Soldier Field"; }
                case "cleftblstdm" : { return "FistEnergy Stadium"; }
                default: { return "Unknown"; }
            }
        }

        public static List<string> CreateForSaleCSVString(List<UplandForSaleProp> forSaleProperties, Dictionary<long, Property> propDictionary, Dictionary<long, string> propBuildings)
        {
            List<string> output = new List<string>();
            output.Add("PropertyId,Price,Currency,Mint,Markup,CityId,Address,Owner,NeighborhoodId,Structure");

            foreach (UplandForSaleProp prop in forSaleProperties)
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
                propString += string.Format("{0:F0},", 100 * prop.SortValue / (propDictionary[prop.Prop_Id].MonthlyEarnings * 12 / 0.1728));
                propString += string.Format("{0},", propDictionary[prop.Prop_Id].CityId);
                propString += string.Format("{0},", propDictionary[prop.Prop_Id].Address);
                propString += string.Format("{0},", prop.Owner);
                propString += string.Format("{0},", propDictionary[prop.Prop_Id].NeighborhoodId.HasValue ? propDictionary[prop.Prop_Id].NeighborhoodId.Value.ToString() : "-1");
                propString += string.Format("{0}", propBuildings.ContainsKey(prop.Prop_Id) ? propBuildings[prop.Prop_Id] : "None");

                output.Add(propString);
            }

            return output;
        }

        public static List<string> ForSaleTxtString(List<UplandForSaleProp> forSaleProps, Dictionary<long, Property> propDictionary, Dictionary<long, string> propBuildings, string reportHeader, string expireDate)
        {
            List<string> output = new List<string>();

            int pricePad = 11;
            int markupPad = 10;
            int mintPad = 10;
            int addressPad = propDictionary.Max(p => p.Value.Address.Length);
            int ownerPad = forSaleProps.Max(p => p.Owner.Length);
            int neighborhoodPad = 14;
            int buildingPad = 29;
            output.Add(string.Format("{0} Data expires at {1}", reportHeader, expireDate));
            output.Add("");
            output.Add(string.Format("{0} - Currency - {1} - {2} - {3} - {4} - {5} - {6}", "Price".PadLeft(pricePad), "Mint".PadLeft(mintPad), "Markup".PadLeft(markupPad), "Address".PadLeft(addressPad), "Owner".PadLeft(ownerPad), "NeighborhoodId".PadLeft(neighborhoodPad), "Building".PadLeft(buildingPad)));

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
                propString += string.Format("{0:N0}", Math.Round(propDictionary[prop.Prop_Id].MonthlyEarnings * 12 / 0.1728)).PadLeft(mintPad);
                propString += " - ";
                propString += string.Format("{0:N0}%", 100 * prop.SortValue / (propDictionary[prop.Prop_Id].MonthlyEarnings * 12 / 0.1728)).PadLeft(markupPad);
                propString += " - ";
                propString += string.Format("{0}", propDictionary[prop.Prop_Id].Address).PadLeft(addressPad);
                propString += " - ";
                propString += string.Format("{0}", prop.Owner).PadLeft(ownerPad);
                propString += " - ";
                propString += string.Format("{0}", propDictionary[prop.Prop_Id].NeighborhoodId.HasValue ? propDictionary[prop.Prop_Id].NeighborhoodId.Value.ToString() : "-1").PadLeft(neighborhoodPad);
                propString += " - ";

                if(propBuildings.ContainsKey(prop.Prop_Id))
                {
                    propString += string.Format("{0}", propBuildings[prop.Prop_Id]).PadLeft(buildingPad);
                }
                else
                {
                    propString += "None".PadLeft(buildingPad);
                }
                output.Add(propString);
            }

            return output;
        }
    }
}
