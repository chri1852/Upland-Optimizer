using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Upland.Types;
using Upland.Types.Types;
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
                case "cleftblstdm" : { return "FirstEnergy Stadium"; }
                default: { return "Unknown"; }
            }
        }

        public static string TranslateUserLevel(int userLevel)
        {
            switch (userLevel)
            {
                case 0: 
                    return "Visitor";
                case 1: 
                    return "Uplander";
                case 2:
                    return "Pro";
                case 3:
                    return "Director";
                case 4:
                    return "Executive";
                case 5:
                    return "Chief Executive";
                case -1:
                    return "All";
                default:
                    return "Unknown";
            }
        }

        public static List<double> GetCityAreaCoordinates(int cityId)
        {
            // N, S, E, W
            switch (cityId)
            {
                case 1:
                    return new List<double> {37.874861, 37.595937, -122.345007, -122.520960};
                case 3:
                    return new List<double> {40.882824, 40.680972,  -73.902522,  -74.052383};
                case 4:
                    return new List<double> {40.659563, 40.627391,  -73.757297,  -73.813173};
                case 5:
                    return new List<double> {36.953982, 36.648627, -119.592434, -119.941636};
                case 6:
                    return new List<double> {40.745700, 40.561811,  -73.827239,  -74.051086};
                case 7:
                    return new List<double> {37.894152, 37.697192, -122.108366, -122.346632};
                case 8:
                    return new List<double> {40.650337, 40.492300,  -74.045197,  -74.264237};
                case 9:
                    return new List<double> {35.431051, 35.253613, -118.797393, -119.190841};
                case 10:
                    return new List<double> {42.032864, 41.638941,  -87.514990,  -87.946204};
                case 11:
                    return new List<double> {41.605164, 41.390193,  -81.519952,  -81.883188};
                case 12:
                    return new List<double> {37.422572, 37.320392, -121.926698, -122.013043};
                case 13:
                    return new List<double> {40.855192, 40.785172,  -74.056121,  -74.130538};
                case 14:
                    return new List<double> {39.359456, 38.823596,  -94.370848,  -94.770819};
                case 15:
                    return new List<double> {30.179050, 29.863468,  -89.621784,  -90.142948};
                case 16:
                    return new List<double> {36.409888, 35.964879,  -86.511996,  -87.060626};
                default:
                    return new List<double> {0, 0, 0, 0};
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
                propString += string.Format("{0:N2}%", 100 * prop.SortValue / (propDictionary[prop.Prop_Id].MonthlyEarnings * 12 / 0.1728)).PadLeft(markupPad);
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

        public static string CreateCollatedStatTextString(CollatedStatsObject statObject)
        {
            string returnString = "";
            returnString += string.Format("{0:N0}", statObject.TotalProps).PadLeft(11);
            returnString += " - ";
            returnString += string.Format("{0:N0}", statObject.LockedProps).PadLeft(12);
            returnString += " - ";
            returnString += string.Format("{0:N0}", statObject.UnlockedNonFSAProps).PadLeft(22);
            returnString += " - ";
            returnString += string.Format("{0:N0}", statObject.UnlockedFSAProps).PadLeft(18);
            returnString += " - ";
            returnString += string.Format("{0:N0}", statObject.ForSaleProps).PadLeft(14);
            returnString += " - ";
            returnString += string.Format("{0:N0}", statObject.OwnedProps).PadLeft(11);
            returnString += " - ";
            returnString += string.Format("{0:N2}%", statObject.PercentMinted).PadLeft(14);
            returnString += " - ";
            returnString += string.Format("{0:N2}%", statObject.PercentNonFSAMinted).PadLeft(23);

            return returnString;
        }

        public static string CreateCollatedStatCSVString(CollatedStatsObject statObject)
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6:F2},{7:F2}", statObject.TotalProps, statObject.LockedProps, statObject.UnlockedNonFSAProps, statObject.UnlockedFSAProps, statObject.ForSaleProps, statObject.OwnedProps, statObject.PercentMinted, statObject.PercentNonFSAMinted);
        }
    }
}
