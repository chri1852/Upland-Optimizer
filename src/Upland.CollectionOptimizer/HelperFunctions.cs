using System;
using System.Collections.Generic;
using System.Linq;
using Upland.Types;

namespace Upland.CollectionOptimizer
{
    public static class HelperFunctions
    {
        public static Dictionary<int, Collection> DeepCollectionClone(Dictionary<int, Collection> collections)
        {
            Dictionary<int, Collection> clonedCollections = new Dictionary<int, Collection>();

            foreach (KeyValuePair<int, Collection> entry in collections)
            {
                clonedCollections.Add(entry.Key, entry.Value.Clone());
            }

            return clonedCollections;
        }

        public static double CalculateBaseMonthlyUPX(Dictionary<long, Property> Properties)
        {
            return Properties.Sum(p => p.Value.MonthlyEarnings);
        }

        public static double CalculateMonthlyUpx(Dictionary<int, Collection> FilledCollections, Dictionary<long, Property> Properties, List<long> SlottedPropertyIds)
        {
            double total = 0;

            total += FilledCollections.Sum(c => c.Value.MonthlyUpx);
            total += Properties.Where(p => !SlottedPropertyIds.Contains(p.Value.Id)).Sum(p => p.Value.MonthlyEarnings);

            return total;
        }

        public static List<Property> BuildHypotheicalProperties(int cityId, int numberOfProperties, double averageMonthlyUpx)
        {
            List<Property> properties = new List<Property>();
            for (int i = 0; i < numberOfProperties; i++)
            {
                Property prop = new Property
                {
                    Id = 10000 + i,
                    Address = string.Format("TEST PROPERTY {0}", i),
                    CityId = cityId,
                    StreetId = -1 * i,
                    Size = 1,
                    MonthlyEarnings = averageMonthlyUpx
                };

                properties.Add(prop);
            }

            return properties;
        }

        #region /* Debug Console Functions */

        public static void WriteCollecitonToConsole(
            Dictionary<int, Collection> FilledCollections,
            Dictionary<long, Property> Properties,
            List<long> SlottedPropertyIds,
            Dictionary<int, Collection> UnfilledCollections,
            Dictionary<int, Collection> UnoptimizedCollections,
            Dictionary<int, Collection> MissingCollections)
        {
            int TotalCollectionRewards = 0;
            List<Collection> collections = FilledCollections.OrderByDescending(c => c.Value.MonthlyUpx).Select(c => c.Value).ToList();
            Console.WriteLine();
            foreach (Collection collection in collections)
            {
                if (!Consts.StandardCollectionIds.Contains(collection.Id))
                {
                    string collectionCity = Consts.Cities[Properties[collection.SlottedPropertyIds[0]].CityId];
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(collectionCity);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(" - ");
                    TotalCollectionRewards += collection.Reward;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Standard");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(" - ");
                }

                WriteCollectionName(collection);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(" - ");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("{0:N2}", collection.MonthlyUpx / collection.Boost);
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(" --> ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("{0:N2}", collection.MonthlyUpx);
                Console.ForegroundColor = ConsoleColor.Cyan;

                foreach (long propertyId in collection.SlottedPropertyIds)
                {
                    Console.WriteLine("     {0}", Properties[propertyId].Address);
                }
            }

            Console.WriteLine();

            string baseMonthlyUpx = string.Format("{0:N2}", CalculateBaseMonthlyUPX(Properties));
            string totalMonthlyUpx = string.Format("{0:N2}", CalculateMonthlyUpx(FilledCollections, Properties, SlottedPropertyIds));

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Base Monthly UPX...........: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(baseMonthlyUpx);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Total Monthly UPX..........: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(totalMonthlyUpx);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Total Collection Reward UPX: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("{0:N2}", TotalCollectionRewards);

            if (UnfilledCollections.Count > 0 || UnoptimizedCollections.Count > 0 || MissingCollections.Count > 0)
            {
                Console.WriteLine("");

                if (UnfilledCollections.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("");
                    Console.WriteLine(string.Format("Unfilled Collections"));
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    foreach (KeyValuePair<int, Collection> entry in UnfilledCollections)
                    {
                        Console.WriteLine(string.Format("     {0} - {1}", entry.Value.Id, entry.Value.Name));
                    }
                }

                if (UnoptimizedCollections.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("");
                    Console.WriteLine("Unoptimized Collections");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    foreach (KeyValuePair<int, Collection> entry in UnoptimizedCollections)
                    {
                        string collectionCity = entry.Value.CityId.HasValue ? Consts.Cities[entry.Value.CityId.Value] : "Standard";
                        Console.WriteLine(string.Format("     {0} - {1} - Missing Props {2}", collectionCity, entry.Value.Name, entry.Value.NumberOfProperties - entry.Value.EligablePropertyIds.Count));
                    }
                }

                if (MissingCollections.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("");
                    Console.WriteLine("Missing Collections");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    foreach (KeyValuePair<int, Collection> entry in MissingCollections)
                    {
                        string collectionCity = entry.Value.CityId.HasValue ? Consts.Cities[entry.Value.CityId.Value] : "Standard";
                        Console.WriteLine(string.Format("     {0} - {1} - Missing Props {2}", collectionCity, entry.Value.Name, entry.Value.NumberOfProperties - entry.Value.EligablePropertyIds.Count));
                    }
                }
            }
            Console.ResetColor();

            Console.WriteLine();
        }

        private static void WriteCollectionName(Collection collection)
        {
            switch (collection.Category)
            {
                case 1:
                    Console.ForegroundColor = ConsoleColor.Blue;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;
                case 3:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 4:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case 5:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
            }
            Console.Write(collection.Name);
            Console.ResetColor();
        }

        public static void WriteCollectionFinishTime(Collection collection)
        {
            WriteCollectionName(collection);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" - ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
        }

        #endregion /* Debug Console Functions */
    }
}
