using System;
using System.Collections.Generic;
using System.Linq;

namespace Startup
{
    public static class HelperFunctions
    {
        public static string GetRandomName(Random random)
        {
            List<string> names = new List<string>
            {
                "Friendo",
                "Chief",
                "Slugger",
                "Boss",
                "Champ",
                "Amigo",
                "Guy",
                "Buddy",
                "Sport",
                "My Dude",
                "Pal",
                "Buddy",
                "Bud",
                "Big Guy",
                "Tiger",
                "Scooter",
                "Shooter",
                "Ace",
                "Partner",
                "Slick",
                "Hombre",
                "Hoss",
                "Bub",
                "Buster",
                "Partner",
                "Fam",
                "Cap",
                "Skip",
                "Slim",
                "Ghost Rider"
            };

            return names[random.Next(names.Count)];
        }

        public static List<string> BreakLongMessage(string message)
        {
            List<string> results = message.Split(Environment.NewLine, StringSplitOptions.None).ToList();
            List<string> stringGroups = new List<string>();
            string splitMessage = "";

            foreach (string entry in results)
            {
                if (splitMessage.Length + entry.Length + 1 < 2000)
                {
                    splitMessage += entry;
                    splitMessage += Environment.NewLine;
                }
                else
                {
                    stringGroups.Add(splitMessage);
                    splitMessage = entry;
                    splitMessage += Environment.NewLine;
                }
            }

            stringGroups.Add(splitMessage);

            return stringGroups;
        }

        public static string GetHelpNumber(string command)
        {
            switch (command.ToUpper())
            {
                case "OPTIMIZERRUN":
                    return "1";
                case "OPTIMIZERSTATUS":
                    return "2";
                case "OPTIMIZERRESULTS":
                    return "3";
                case "COLLECTIONINFO":
                    return "4";
                case "PROPERTYINFO":
                    return "5";
                case "NEIGHBORHOODINFO":
                    return "6";
                case "CITYINFO":
                    return "7";
                case "STREETINFO":
                    return "8";
                case "SUPPORTME":
                    return "9";
                case "COLLECTIONSFORSALE":
                    return "10";
                case "NEIGHBORHOODSFORSALE":
                    return "11";
                case "CITYSFORSALE":
                    return "12";
                case "BUILDINGSFORSALE":
                    return "13";
                case "STREETSFORSALE":
                    return "14";
                case "UNMINTEDPROPERTIES":
                    return "15";
                case "ALLPROPERTIES":
                    return "16";
                case "SEARCHSTREETS":
                    return "17";
                case "SEARCHPROPERTIES":
                    return "18";
                case "SEARCHNEIGHBORHOODS":
                    return "19";
                case "GETASSETS":
                    return "20";
                case "GETSALESHISTORY":
                    return "21";
                case "APPRAISAL":
                    return "22";
                case "HOWMANYRUNS":
                    return "23";
                case "OPTIMIZERLEVELRUN":
                    return "24";
                case "OPTIMIZERWHATIFRUN":
                    return "25";
                case "OPTIMIZEREXCLUDERUN":
                    return "26";
                default:
                    return "0";
            }
        }

        public static List<string> GetHelpTextForCommandNumber(string number)
        {
            List<string> helpOutput = new List<string>();
            switch (number)
            {
                case "1":
                    helpOutput.Add(string.Format("!OptimizerRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will start an optimizer run for you. Standard users get 6 free runs, while supporters can run this as many times as they like. To get your results or check on the status, run the !OptimizerResults or !OptimizerStatus commands. The first time your run the optimizer it may take some extra time as the system retrieves your property information from Upland."));
                    break;
                case "2":
                    helpOutput.Add(string.Format("!OptimizerStatus"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return the status of your current run, it can be either In Progress, Failed, or Completed. If it fails reach out to Grombrindal."));
                    break;
                case "3":
                    helpOutput.Add(string.Format("!OptimizerResults"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with the results of your optimizer run. It will also list off Unfilled Collections, which you can fill, but the algorithm decided not to, and Unoptimized Collections, which you own at least one property in, but not enough to fill them."));
                    break;
                case "4":
                    helpOutput.Add(string.Format("!CollectionInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with information on all collections, as well as the most recent mint percent, and property status counts. Note that the property count does not include any locked properties, city and standard collections will also have a property count of 0."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionInfo");
                    helpOutput.Add("This command will return a text file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "5":
                    helpOutput.Add(string.Format("!PropertyInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a csv file with all of your properties, or the properties of the specified user."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !PropertyInfo");
                    helpOutput.Add("This command will return a text file with your properties.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !PropertyInfo Hornbrod txt");
                    helpOutput.Add("This command will return a text file with all the properties owned by Hornbrod.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !PropertyInfo Hornbrod csv");
                    helpOutput.Add("This command will return a csv file with all the properties owned by Hornbrod.");
                    break;
                case "6":
                    helpOutput.Add(string.Format("!NeighborhoodInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with information on all Neighborhoods, as well as the most recent mint percent, and property status counts."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodInfo");
                    helpOutput.Add("This command will return a text file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "7":
                    helpOutput.Add(string.Format("!CityInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with all cityIds and Names, as well as the most recent mint percent, and property status counts."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CityInfo");
                    helpOutput.Add("This command will return a text file");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CityInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "8":
                    helpOutput.Add(string.Format("!StreetInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with all streetIds and Names, as well as the most recent mint percent, and property status counts. Note you probably want this one to return as a CSV as there is a lot of street data."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetInfo");
                    helpOutput.Add("This command will return a text file");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "9":
                    helpOutput.Add(string.Format("!SupportMe"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will let you know how to support the development of this tool."));
                    break;
                case "10":
                    helpOutput.Add(string.Format("!CollectionsForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props for sale in the given collection id (not standard or city collections), and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionsForSale 177 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties for sale for UPX in the Kansas City Main St collection, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionsForSale 177 PRICE ALL");
                    helpOutput.Add("The above command finds all properties for sale in the Kansas City Main St collection, and returns a csv file from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionsForSale 177 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties for sale for USD in the Kansas City Main St collection, and returns a txt file from from lowest to greatest price.");
                    break;
                case "11":
                    helpOutput.Add(string.Format("!NeighborhoodsForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props for sale in the given neighborhood id, and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodsForSale 810 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties for sale for UPX in the Chicago Ashburn neighborhood, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodsForSale 810 PRICE ALL");
                    helpOutput.Add("The above command finds all properties for sale in the Chicago Ashburn neighborhood, and returns a csv from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodsForSale 810 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties for sale for USD in the Chicago Ashburn neighborhood, and returns a txt file from lowest to greatest price.");
                    break;
                case "12":
                    helpOutput.Add(string.Format("!CitysForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props for sale in the given city id, and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CitysForSale 10 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties for sale for UPX in the Chicago, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CitysForSale 1 PRICE ALL");
                    helpOutput.Add("The above command finds all properties for sale in the San Francisco, and returns a csv from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CitysForSale 10 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties for sale for USD in the Chicago, and returns a txt file from lowest to greatest price.");
                    break;
                case "13":
                    helpOutput.Add(string.Format("!BuildingsForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props with Buildings for sale in the given type and id, and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !BuildingsForSale City 1 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties with buildings for sale for UPX in San Francisco, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !BuildingsForSale Street 31898 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties with buildings for sale on Broadway in Nashville for UPX, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !BuildingsForSale Neighborhood 876 PRICE ALL");
                    helpOutput.Add("The above command finds all properties with buildings for sale in the Chicago Portage Park neighborhood, and returns a csv file from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !BuildingsForSale Collection 2 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties with buildings for sale for USD in the Mission District Collection, and returns a txt file from lowest to greatest price.");
                    break;
                case "14":
                    helpOutput.Add(string.Format("!StreetsForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props for sale on the given street id, and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetsForSale 31898 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties for sale for UPX on Broadway in Nashville, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetsForSale 31898 PRICE ALL");
                    helpOutput.Add("The above command finds all properties for sale on Broadway in Nashville, and returns a csv from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetsForSale 28029 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties for sale for USD on Main St in Kansas City, and returns a txt file from lowest to greatest price.");
                    break;
                case "15":
                    helpOutput.Add(string.Format("!UnmintedProperties"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find unminted properties in the given type and id, and return a csv file listing in order of mint price."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !UnmintedProperties City 1 FSA");
                    helpOutput.Add("The above command finds all FSA unminted properties in San Francisco, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !UnmintedProperties Street 31898 NONFSA");
                    helpOutput.Add("The above command finds all non-FSA unminted properties on Broadway in Nashville, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !UnmintedProperties Neighborhood 876 ALL");
                    helpOutput.Add("The above command finds all unminted properties in the Chicago Portage Park neighborhood, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !UnmintedProperties Collection 2 ALL TXT");
                    helpOutput.Add("The above command finds all unminted properties in the Mission District Collection, and returns a txt file from lowest to greatest mint price.");
                    break;
                case "16":
                    helpOutput.Add(string.Format("!AllProperties"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find all properties in the given type and id, and return a csv file listing in order of mint price. On the city level this will only work for Rutherford and Santa Clara."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !AllProperties City 12");
                    helpOutput.Add("The above command finds all FSA unminted properties in Santa Clara, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !AllProperties Street 31898");
                    helpOutput.Add("The above command finds all FSA unminted properties on Broadway in Nashville, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !AllProperties Neighborhood 876");
                    helpOutput.Add("The above command finds all properties in the Chicago Portage Park neighborhood, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !AllProperties Collection 2 TXT");
                    helpOutput.Add("The above command finds all properties in the Mission District Collection, and returns a txt file from lowest to greatest mint price.");
                    break;
                case "17":
                    helpOutput.Add(string.Format("!SearchStreets"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command searches for streets with the given name, and return a txt file with the matching street names."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchStreets Main");
                    helpOutput.Add("The above command finds all streets with MAIN in their name and returns a txt file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchStreets Broadway csv");
                    helpOutput.Add("The above command finds all streets with BROADWAY in their name, and returns a csv file.");
                    break;
                case "18":
                    helpOutput.Add(string.Format("!SearchProperties"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command searches for properties with the given cityId and address, and return a txt file with the matching street names."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchProperties 10 \"Michigan\"");
                    helpOutput.Add("The above command finds all properties in Chicago with Michigan in their address and returns a txt file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchProperties 0 \"3101 W\"");
                    helpOutput.Add("The above command finds all properties in all cities with 3101 W in their address, and returns a txt file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchProperties 29 \"Fordham\" csv");
                    helpOutput.Add("The above command finds all properties in the Bronx with Fordham in their address, and returns a csv file.");
                    break;
                case "19":
                    helpOutput.Add(string.Format("!SearchNeighborhoods"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command searches for neighborhoods with the given name, and return a txt file with the matching neighborhoods."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchNeighborhoods Chester");
                    helpOutput.Add("The above command finds all neighborhoods with CHESTER in their name and returns a txt file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchNeighborhoods South csv");
                    helpOutput.Add("The above command finds all neighborhoods with SOUTH in their name, and returns a csv file.");
                    break;
                case "20":
                    helpOutput.Add(string.Format("!GetAssets"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command returns the assets of the given type owned by the given username."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !GetAssets Hornbrod NFLPA");
                    helpOutput.Add("The above command finds all NFLPA Legits owned by Hornbrod and returns a txt file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !GetAssets Hornbrod Spirit");
                    helpOutput.Add("The above command finds all Spirit Legits owned by Hornbrod and returns a txt file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !GetAssets Hornbrod BlockExplorer");
                    helpOutput.Add("The above command finds all Block Explorers owned by Hornbrod and returns a txt file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !GetAssets Hornbrod Decoration CSV");
                    helpOutput.Add("The above command finds all Decorations owned by Hornbrod and returns a csv file.");
                    break;
                case "21":
                    helpOutput.Add(string.Format("!GetSalesHistory"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command returns the sales history for a given type (City, Neighborhood, Collection, Street, Property, Buyer, Seller) and its identifier. Note this does not include property swaps, only upx and fiat transactions."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !GetSalesHistory City 13");
                    helpOutput.Add("The above command finds the sales history for Rutherford, and returns a csv file sorted by date.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !GetSalesHistory Street 31898");
                    helpOutput.Add("The above command finds the sales history on Broadway in Nashville, and returns a csv file sorted by date.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !GetSalesHistory Buyer hornbrod TXT");
                    helpOutput.Add("The above command finds the sales history where the user hornbrod was a buyer, and returns a txt file sorted by date");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !GetSalesHistory Property \"10, 9843 S Exchange Ave\" TXT");
                    helpOutput.Add("The above command finds the sales history for 9843 S Exchange Ave in Chicago, and returns a txt file sorted by date");
                    break;
                case "22":
                    helpOutput.Add(string.Format("!Appraisal"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command returns an appraisal for all your properties based on the last 4 weeks of market sales, and the floor. Non-supporters will only be able to run this a limited number of times before needing to earn more runs by sending 500 upx to the locations in the locations channel. Note Ultra-Rares, very large properties, or properties in areas with low numbers of sales might have strange numbers."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !Appraisal");
                    helpOutput.Add("The above command runs your appraisal and returns a text file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !Appraisal csv");
                    helpOutput.Add("The above command runs your appraisal and returns a csv file.");
                    helpOutput.Add("");
                    break;
                case "23":
                    helpOutput.Add(string.Format("!HowManyRuns"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command lets you know how many runs you have used, and how to get more."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !HowManyRuns");
                    helpOutput.Add("This command lets you know how many runs you have used, and how to get more.");
                    helpOutput.Add("");
                    break;
                case "24":
                    helpOutput.Add(string.Format("!OptimizerLevelRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will run an optimizer run with a level you specify between 3 and 10. Levels 9 and especially 10 can take quite some time to run. You can get the results and check the status with the standard !OptimizerStatus and !OptimizerResults commands."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !OptimizerLevelRun 5");
                    break;
                case "25":
                    helpOutput.Add(string.Format("!OptimizerWhatIfRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will run an optimizer run with some additional fake properties in the requested collection. You will need to specify the collection Id to add the properties to, the number of properties to add, and the average monthly upx of the properties. You can get the results and check the status with the standard !OptimizerStatus and !OptimizerResults commands."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !OptimizerWhatIfRun 188 3 250.10");
                    helpOutput.Add("The above command will run a WhatIfRun with your current properties, and 3 fake properties in the French Quarter collection with an average monthly upx earnings of 250.10 upx.");
                    break;
                case "26":
                    helpOutput.Add(string.Format("!OptimizerExcludeRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will run an optimizer run and exclude a list of collectionIds seperated by a comma from optimization. You can get the results and check the status with the standard !OptimizerStatus and !OptimizerResults commands."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !OptimizerExcludeRun 222,80");
                    helpOutput.Add("The above command will run a Exclude run ignoring the Bronx Riverdale, and Oakland Grand Ave collections.");
                    break;
                default:
                    helpOutput.Add(string.Format("Not sure what command you are refering to. Try running my !Help command."));
                    break;
            }

            return helpOutput;
        }
    }
}
