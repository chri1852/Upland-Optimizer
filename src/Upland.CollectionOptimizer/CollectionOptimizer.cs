using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Upland.Infrastructure.LocalData;
using Upland.Types;

namespace Upland.CollectionOptimizer
{
    public class CollectionOptimizer
    {
        // SEARCH TODO
        private Dictionary<long, Property> Properties;
        private Dictionary<int, Collection> Collections;
        private List<long> SlottedPropertyIds;
        private Dictionary<int, Collection> FilledCollections;

        private Dictionary<int, Collection> AllCollections;

        private LocalDataManager localDataManager;

        public CollectionOptimizer()
        {
            this.Properties = new Dictionary<long, Property>();
            this.Collections = new Dictionary<int, Collection>();
            this.SlottedPropertyIds = new List<long>();
            this.FilledCollections = new Dictionary<int, Collection>();

            PopulateAllCollections();

            this.localDataManager = new LocalDataManager();
        }

        public async Task OptimizerStartPoint()
        {
            string username;
            string qualityLevel;
            string repeat;

            // Optimizer Quality goes from 1 to 8, you can run it higher but it will slow greatly expecially on larger profiles.
            // For reference here is the output for ben68, net worth 82,216,080.62, with 4,094 properties at various quality levels

            // |  Monthly UPX  | Quality |     Time     |
            // | 1,686,660.00  |    -    |       -      |
            // | 1,672,300.00  |    1    | 00:00:00.593 |
            // | 1,679,993.64  |    2    | 00:00:00.843 |
            // | 1,680,027.55  |    3    | 00:00:01.483 |
            // | 1,680,027.55  |    4    | 00:00:04.199 |
            // | 1,688,251.14  |    5    | 00:00:11.071 |
            // | 1,688,285.05  |    6    | 00:00:53.180 |
            // | 1,688,725.65  |    7    | 00:09:43.214 |
            // | 1,688,725.65  |    8    | 01:09:59.996 |

            Console.WriteLine();
            Console.WriteLine("Collection Optimizer");
            Console.WriteLine();
            do
            {
                Console.Write("Enter the Upland Username: ");
                username = Console.ReadLine();
                Console.Write("Enter the Level (1-8)....: ");
                qualityLevel = Console.ReadLine();
                Console.WriteLine();

                try
                {
                    await RunOptimization(username, int.Parse(qualityLevel));
                }
                catch
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("An Error Occured. Bad Username Perhaps?");
                    Console.ResetColor();
                    Console.WriteLine();
                }

                Console.Write("Run Script Again (Y/N)...: ");
                repeat = Console.ReadLine();
                Console.WriteLine();
            } while (repeat.ToUpper() != "N" && repeat.ToUpper() != "NO" && repeat.ToUpper() != "0");
        }

        /*
        private Dictionary<int, Collection> GetConflictingCollections(int qualityLevel)
        {
            int firstCollection = 0;

            if (this.Collections.Count <= qualityLevel)
            {
                return HelperFunctions.DeepCollectionClone(this.Collections);
            }

            Dictionary<int, Collection> conflictingCollections = new Dictionary<int, Collection>();

            foreach (KeyValuePair<int, Collection> entry in this.Collections.OrderByDescending(c => c.Value.MonthlyUpx))
            {
                // Don't use City Pro of King of the String as the first collection
                if (Consts.StandardCollectionIds.Contains(entry.Value.Id))
                {
                    continue;
                }

                firstCollection = entry.Value.Id;
                conflictingCollections.Add(entry.Value.Id, entry.Value.Clone());
                break;
            }

            // We now have one collection lets loop through all collections til we have all the conflicts.
            foreach (KeyValuePair<int, Collection> entry in this.Collections.OrderByDescending(c => c.Value.MonthlyUpx))
            {
                // if its city pro or King of the street, or any eligable props on collection match eligable props on another collections
                if (!conflictingCollections.ContainsKey(entry.Value.Id) 
                    && DoesCollectionConflictWithList(entry.Value, conflictingCollections)
                    && conflictingCollections.Count < qualityLevel)
                {
                    conflictingCollections.Add(entry.Key, entry.Value.Clone());
                }
            }

            return conflictingCollections;
        }
        */

        private async Task RunOptimization(string username, int qualityLevel)
        {
            await PopulatePropertiesAndCollections(username);
            this.Collections = RebuildCollections(this.Collections, new List<long>());
            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (this.Collections.Count > 0)
            {
                
                Dictionary<int, Collection> conflictingCollections = HelperFunctions.DeepCollectionClone(new Dictionary<int, Collection>(
                    this.Collections.OrderByDescending(c => c.Value.MonthlyUpx).Take(qualityLevel)
                ));
                
                //Dictionary<int, Collection> conflictingCollections = GetConflictingCollections(qualityLevel);

                int collectionToSlotId = RecursionWrapper(conflictingCollections);

                WriteCollectionFinishTime(this.Collections[collectionToSlotId]);

                SetFilledCollection(collectionToSlotId);
            }

            timer.Stop();

            BuildBestNewbieCoollection();

            List<string> outputStrings = new List<string>();
            outputStrings.Add(string.Format("Collection Optimization Report - {0}", DateTime.Now.ToString("MM-dd-yyyy")));
            outputStrings.Add("-------------------------------------------");
            outputStrings.Add(string.Format("Ran for {0} at Quality Level {1}", username, qualityLevel));
            outputStrings.Add(string.Format("Run Time - {0}", timer.Elapsed));
            outputStrings.Add("");
            outputStrings = WriteCollecitonToConsole(outputStrings);

            File.WriteAllLines(string.Format("{0}\\{1}_lvl{2}_{3}.txt", Consts.OuputFolder, username, qualityLevel, DateTime.Now.ToString("MM-dd-yyyy")), outputStrings);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("Total Time - ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("{0}", timer.Elapsed);
        }

        private int RecursionWrapper(Dictionary<int, Collection> conflictingCollections)
        {
            double maxMonthly = 0;
            int maxCollectionId = -1;
            List<long> ignorePropertyIds = new List<long>();

            foreach (KeyValuePair<int, Collection> entry in conflictingCollections)
            {
                Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(conflictingCollections);
                double collectionMax = RecursiveMaxUpxFinder(copiedCollections, entry.Value, ignorePropertyIds);

                if (collectionMax > maxMonthly
                    || (collectionMax == maxMonthly && (maxCollectionId == 1 || maxCollectionId == 21)))
                {
                    maxMonthly = collectionMax;
                    maxCollectionId = entry.Value.Id;
                }
            }

            return maxCollectionId;
        }

        private bool DoesCollectionConflictWithList(Collection collection, IEnumerable<KeyValuePair<int, Collection>> conflictingList)
        {
            return Consts.StandardCollectionIds.Contains(collection.Id) ||
                conflictingList.Any(e => e.Value.EligablePropertyIds.Any(p => collection.EligablePropertyIds.Contains(p)));
        }

        private bool TESTAnyCollectionConflict(Dictionary<int, Collection> collections)
        {
            foreach (KeyValuePair<int, Collection> entry in collections)
            {
                if(DoesCollectionConflictWithList(entry.Value, collections.Where(c => c.Key != entry.Value.Id)))
                {
                    return true;
                }
            }

            return false;
        }

        private double RecursiveMaxUpxFinder(Dictionary<int, Collection> collections, Collection collection, List<long> ignorePropertyIds)
        {
            if (collections == null || collections.Count <= 1)
            {
                return collection.MonthlyUpx;
            }
            else
            {
                if(!TESTAnyCollectionConflict(collections))
                {
                    return collections.Sum(c => c.Value.MonthlyUpx);
                }
                List<long> copiedIgnorePropertyIds = new List<long>(ignorePropertyIds);
                Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(new Dictionary<int, Collection>(
                    collections.Where(c => c.Value.Id != collection.Id)
                ));
                copiedIgnorePropertyIds.AddRange(collection.SlottedPropertyIds);

                copiedCollections = RemoveIdsFromCollections(copiedCollections, collection.SlottedPropertyIds);
                copiedCollections = RebuildCollections(copiedCollections, copiedIgnorePropertyIds);

                double maxMonthly = 0;
                foreach (KeyValuePair<int, Collection> entry in copiedCollections)
                {
                    double newMax = RecursiveMaxUpxFinder(copiedCollections, entry.Value, copiedIgnorePropertyIds);
                    if (newMax > maxMonthly)
                    {
                        maxMonthly = newMax;
                    }
                }

                return collection.MonthlyUpx + maxMonthly;
            }
        }

        private void PopulateAllCollections()
        {
            this.AllCollections = new Dictionary<int, Collection>(); ;
            LocalDataManager dataManager = new LocalDataManager();
            List<Collection> collections = dataManager.GetCollections();

            foreach (Collection collection in collections)
            {
                this.AllCollections.Add(collection.Id, collection);
            }
        }

        private async Task PopulatePropertiesAndCollections(string username)
        {
            this.Collections = new Dictionary<int, Collection>();
            this.FilledCollections = new Dictionary<int, Collection>();
            this.SlottedPropertyIds = new List<long>();
            this.Properties = new Dictionary<long, Property>();

            Dictionary<int, Collection> collections = HelperFunctions.DeepCollectionClone(this.AllCollections);

            List<Property> userProperties = await this.localDataManager.GetPropertysByUsername(username);

            userProperties = userProperties.OrderByDescending(p => p.MonthlyEarnings).ToList();

            foreach (KeyValuePair<int, Collection> entry in collections)
            {
                foreach (Property property in userProperties)
                {
                    if ((!Consts.StandardCollectionIds.Contains(entry.Value.Id) && !Consts.CityCollectionIds.Contains(entry.Value.Id) && entry.Value.MatchingPropertyIds.Contains(property.Id))
                        || (Consts.CityCollectionIds.Contains(entry.Value.Id) && entry.Value.CityId == property.CityId))
                    {
                        entry.Value.EligablePropertyIds.Add(property.Id);
                    }
                }

                if (entry.Value.EligablePropertyIds.Count >= entry.Value.NumberOfProperties)
                {
                    this.Collections.Add(entry.Value.Id, entry.Value);
                }

                if (entry.Value.Name == Consts.CityPro || entry.Value.Name == Consts.KingOfTheStreet)
                {
                    this.Collections.Add(entry.Value.Id, entry.Value);
                }
            }

            foreach (Property property in userProperties)
            {
                this.Properties.Add(property.Id, property);
            }
        }

        private Dictionary<int, Collection> RebuildCollections(Dictionary<int, Collection> collections, List<long> ignorePropertyIds)
        {
            Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(collections);
            List<int> removeCollectionIds = new List<int>();

            foreach (KeyValuePair<int, Collection> entry in copiedCollections)
            {
                entry.Value.SlottedPropertyIds = new List<long>();
                entry.Value.MonthlyUpx = 0;

                foreach (long id in entry.Value.EligablePropertyIds)
                {
                    if (entry.Value.SlottedPropertyIds.Count < entry.Value.NumberOfProperties && !ignorePropertyIds.Contains(id))
                    {
                        entry.Value.SlottedPropertyIds.Add(id);
                        entry.Value.MonthlyUpx += this.Properties[id].MonthlyEarnings * entry.Value.Boost;
                    }
                }

                if (entry.Value.SlottedPropertyIds.Count < entry.Value.NumberOfProperties
                    && entry.Value.Name != Consts.CityPro
                    && entry.Value.Name != Consts.KingOfTheStreet)
                {
                    removeCollectionIds.Add(entry.Value.Id);
                }
            }

            foreach (int id in removeCollectionIds)
            {
                copiedCollections.Remove(id);
            }

            copiedCollections = BuildBestCityProCollection(copiedCollections, ignorePropertyIds);
            copiedCollections = BuildBestKingOfTheStreetCollection(copiedCollections, ignorePropertyIds);

            return copiedCollections;
        }

        private void SetFilledCollection(int collectionId)
        {
            this.SlottedPropertyIds.AddRange(this.Collections[collectionId].SlottedPropertyIds);
            this.FilledCollections.Add(collectionId, this.Collections[collectionId]);
            this.Collections.Remove(collectionId);

            this.Collections = RemoveIdsFromCollections(this.Collections, this.FilledCollections[collectionId].SlottedPropertyIds);
            this.Collections = RebuildCollections(this.Collections, new List<long>());
        }

        private Dictionary<int, Collection> RemoveIdsFromCollections(Dictionary<int, Collection> collections, List<long> ids)
        {
            Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(collections);

            foreach (KeyValuePair<int, Collection> entry in copiedCollections)
            {
                entry.Value.SlottedPropertyIds.RemoveAll(i => ids.Contains(i));
                entry.Value.EligablePropertyIds.RemoveAll(i => ids.Contains(i));
            }

            return copiedCollections;
        }

        private Dictionary<int, Collection> BuildBestCityProCollection(Dictionary<int, Collection> collections, List<long> ignorePropertyIds)
        {
            if (!collections.Any(c => c.Value.Name == Consts.CityPro))
            {
                return collections;
            }

            Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(collections);
            Dictionary<int, StandardCollectionBuilder> cityProCollections = new Dictionary<int, StandardCollectionBuilder>();

            foreach (KeyValuePair<long, Property> entry in this.Properties)
            {
                if (this.SlottedPropertyIds.Contains(entry.Value.Id) || ignorePropertyIds.Contains(entry.Value.Id))
                {
                    continue;
                }

                if (!cityProCollections.Any(c => c.Key == entry.Value.CityId))
                {
                    StandardCollectionBuilder newCityCollection = new StandardCollectionBuilder
                    {
                        Id = entry.Value.CityId,
                        PropIds = new List<long>(),
                        MonthlyUpx = 0
                    };
                    cityProCollections.Add(newCityCollection.Id, newCityCollection);
                }

                if (cityProCollections[entry.Value.CityId].PropIds.Count < 5)
                {
                    cityProCollections[entry.Value.CityId].PropIds.Add(entry.Value.Id);
                    cityProCollections[entry.Value.CityId].MonthlyUpx += entry.Value.MonthlyEarnings * Consts.CityProBoost;
                }
            }

            if (cityProCollections.Any(c => c.Value.PropIds.Count == 5))
            {
                StandardCollectionBuilder topCityProCollection = cityProCollections
                    .Where(c => c.Value.PropIds.Count == 5)
                    .OrderByDescending(c => c.Value.MonthlyUpx).First().Value;
                copiedCollections[Consts.CityProId].SlottedPropertyIds = topCityProCollection.PropIds;
                copiedCollections[Consts.CityProId].MonthlyUpx = topCityProCollection.MonthlyUpx;
            }
            else
            {
                copiedCollections.Remove(Consts.CityProId);
            }

            return copiedCollections;
        }

        private Dictionary<int, Collection> BuildBestKingOfTheStreetCollection(Dictionary<int, Collection> collections, List<long> ignorePropertyIds)
        {
            if (!collections.Any(c => c.Value.Name == Consts.KingOfTheStreet))
            {
                return collections;
            }

            Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(collections);
            Dictionary<int, StandardCollectionBuilder> kingOfTheStreetCollections = new Dictionary<int, StandardCollectionBuilder>();

            foreach (KeyValuePair<long, Property> entry in this.Properties)
            {
                if (this.SlottedPropertyIds.Contains(entry.Value.Id) || ignorePropertyIds.Contains(entry.Value.Id) || entry.Value.StreetId <= 0)
                {
                    continue;
                }

                if (!kingOfTheStreetCollections.Any(c => c.Key == entry.Value.StreetId))
                {
                    StandardCollectionBuilder newStreetCollection = new StandardCollectionBuilder
                    {
                        Id = entry.Value.StreetId,
                        PropIds = new List<long>(),
                        MonthlyUpx = 0
                    };
                    kingOfTheStreetCollections.Add(newStreetCollection.Id, newStreetCollection);
                }

                if (kingOfTheStreetCollections[entry.Value.StreetId].PropIds.Count < 3)
                {
                    kingOfTheStreetCollections[entry.Value.StreetId].PropIds.Add(entry.Value.Id);
                    kingOfTheStreetCollections[entry.Value.StreetId].MonthlyUpx += entry.Value.MonthlyEarnings * Consts.KingOfTheStreetBoost;
                }
            }

            if (kingOfTheStreetCollections.Any(c => c.Value.PropIds.Count == 3))
            {
                StandardCollectionBuilder topKingOfTheStreetCollection = kingOfTheStreetCollections
                    .Where(c => c.Value.PropIds.Count == 3)
                    .OrderByDescending(c => c.Value.MonthlyUpx).First().Value;
                copiedCollections[Consts.KingOfTheStreetId].SlottedPropertyIds = topKingOfTheStreetCollection.PropIds;
                copiedCollections[Consts.KingOfTheStreetId].MonthlyUpx = topKingOfTheStreetCollection.MonthlyUpx;
            }
            else
            {
                copiedCollections.Remove(Consts.KingOfTheStreetId);
            }

            return copiedCollections;
        }

        private void BuildBestNewbieCoollection()
        {
            Collection newbie = new Collection
            {
                Id = Consts.NewbieId,
                Name = Consts.Newbie,
                Boost = Consts.NewbieBoost,
                NumberOfProperties = 1,
                SlottedPropertyIds = new List<long>(),
                MonthlyUpx = 0,
                Category = 1
            };

            Property largestUnslotted = this.Properties.Where(p => !this.SlottedPropertyIds.Contains(p.Value.Id))?.FirstOrDefault().Value;

            if (largestUnslotted != null)
            {
                newbie.SlottedPropertyIds.Add(largestUnslotted.Id);
                newbie.MonthlyUpx += largestUnslotted.MonthlyEarnings * 1.1;
                this.SlottedPropertyIds.Add(largestUnslotted.Id);
                this.FilledCollections.Add(newbie.Id, newbie);
            }
        }

        private List<string> WriteCollecitonToConsole(List<string> outputStrings)
        {
            int TotalCollectionRewards = 0;
            List<Collection> collections = this.FilledCollections.OrderByDescending(c => c.Value.MonthlyUpx).Select(c => c.Value).ToList();
            Console.WriteLine();
            foreach (Collection collection in collections)
            {
                if(!Consts.StandardCollectionIds.Contains(collection.Id))
                {
                    string collectionCity = Consts.Cities[this.Properties[collection.SlottedPropertyIds[0]].CityId];
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write(collectionCity);
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(" - ");
                    outputStrings.Add(string.Format("{0} - {1} - {2:N2} --> {3:N2}", collectionCity, collection.Name, collection.MonthlyUpx / collection.Boost, collection.MonthlyUpx));
                    TotalCollectionRewards += collection.Reward;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("Standard");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write(" - ");
                    outputStrings.Add(string.Format("Standard - {0} - {1:N2} --> {2:N2}", collection.Name, collection.MonthlyUpx / collection.Boost, collection.MonthlyUpx));
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
                    outputStrings.Add(string.Format("     {0}", this.Properties[propertyId].Address));
                    Console.WriteLine("     {0}", this.Properties[propertyId].Address);
                }
                outputStrings.Add("");
            }

            Console.WriteLine();

            string baseMonthlyUpx = string.Format("{0:N2}", CalulateBaseMonthlyUPX());
            string totalMonthlyUpx = string.Format("{0:N2}", CalcualteMonthylUpx());

            outputStrings.Add("");
            outputStrings.Add(string.Format("Base Monthly UPX...........: {0}", baseMonthlyUpx));
            outputStrings.Add(string.Format("Total Monthly UPX..........: {0}", totalMonthlyUpx));
            outputStrings.Add("");
            outputStrings.Add(string.Format("Total Collection Reward UPX: {0:N2}", TotalCollectionRewards));

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
            Console.ResetColor();

            Console.WriteLine();

            return outputStrings;
        }

        private void WriteCollectionName(Collection collection)
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

        private void WriteCollectionFinishTime(Collection collection)
        {
            WriteCollectionName(collection);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" - ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss"));
        }

        private double CalulateBaseMonthlyUPX()
        {
            return this.Properties.Sum(p => p.Value.MonthlyEarnings);
        }

        private double CalcualteMonthylUpx()
        {
            double total = 0;

            total += this.FilledCollections.Sum(c => c.Value.MonthlyUpx);
            total += this.Properties.Where(p => !this.SlottedPropertyIds.Contains(p.Value.Id)).Sum(p => p.Value.MonthlyEarnings);

            return total;
        }
    }
}
