using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Upland.Infrastructure.Up2LandApi;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using Upland.Types.Up2LandApiTypes;
using Upland.Types.UplandApiTypes;

namespace Upland.CollectionOptimizer
{
    public class CollectionOptimizer
    {
        // SEARCH TODO
        private Dictionary<long, Up2LandProperty> Properties;
        private Dictionary<int, Collection> Collections;
        private List<long> SlottedPropertyIds;
        private Dictionary<int, Collection> FilledCollections;

        private Dictionary<int, Collection> AllCollections;
        private Dictionary<int, string> AllCities;
        private bool UseAlternateCollectionOptimization;

        private Up2LandApiRepository up2LandApiRepository;

        public CollectionOptimizer(Dictionary<int, Collection> collections, Dictionary<int, string> cities, bool useAlternateCollectionOptimization)
        {
            this.Properties = new Dictionary<long, Up2LandProperty>();
            this.Collections = new Dictionary<int, Collection>();
            this.SlottedPropertyIds = new List<long>();
            this.FilledCollections = new Dictionary<int, Collection>();

            this.AllCollections = HelperFunctions.DeepCollectionClone(collections);
            this.AllCities = cities;
            this.UseAlternateCollectionOptimization = useAlternateCollectionOptimization;

            this.up2LandApiRepository = new Up2LandApiRepository();
        }

        public async Task RunOptimization(string username, int qualityLevel)
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

            if (this.UseAlternateCollectionOptimization)
            {
                ignorePropertyIds = AddPropIdsToIgnoreForCollectionsNotInProcess(conflictingCollections);
                conflictingCollections = RebuildCollections(conflictingCollections, ignorePropertyIds);
            }

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

        private List<long> AddPropIdsToIgnoreForCollectionsNotInProcess(Dictionary<int, Collection> collections)
        {
            List<long> ignorePropertyIds = new List<long>();

            foreach (KeyValuePair<int, Collection> entry in this.Collections)
            {
                if (!collections.Any(c => c.Value.Id == entry.Value.Id))
                {
                    ignorePropertyIds.AddRange(entry.Value.SlottedPropertyIds);
                }
            }

            return ignorePropertyIds;
        }

        private double RecursiveMaxUpxFinder(Dictionary<int, Collection> collections, Collection collection, List<long> ignorePropertyIds)
        {
            if (collections == null || collections.Count <= 1)
            {
                return collection.MonthlyUpx;
            }
            else
            {
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

        private async Task PopulatePropertiesAndCollections(string username)
        {
            this.Collections = new Dictionary<int, Collection>();
            this.FilledCollections = new Dictionary<int, Collection>();
            this.SlottedPropertyIds = new List<long>();
            this.Properties = new Dictionary<long, Up2LandProperty>();

            Dictionary<int, Collection> collections = HelperFunctions.DeepCollectionClone(this.AllCollections);

            List<Up2LandProperty> userProperties = await this.up2LandApiRepository.GetUserProperties(username);

            userProperties = userProperties.OrderByDescending(p => p.Mint_Price).ToList();

            foreach (KeyValuePair<int, Collection> entry in collections)
            {
                foreach (Up2LandProperty property in userProperties)
                {
                    property.collections.RemoveAll(r => r.Id == Consts.CityProId || r.Id == Consts.KingOfTheStreetId || r.Id == Consts.NewbieId);
                    /*
                    if (entry.Value.CityIds.Contains(property.City_Id)
                        || entry.Value.StreetIds.Contains(property.Street_Id)
                        || entry.Value.NeighborhoodIds.Contains(property.Neighborhood_Id)
                        || entry.Value.MatchingPropertyIds.Contains(property.Prop_Id))
                    */
                    if ((!Consts.StandardAndCityCollectionIds.Contains(entry.Value.Id) && entry.Value.MatchingPropertyIds.Contains(property.Prop_Id))
                        || (Consts.StandardAndCityCollectionIds.Contains(entry.Value.Id) && !HelperFunctions.IsCollectionStd(entry.Value) && entry.Value.CityIds.Contains(property.City_Id)))
                    {
                        entry.Value.EligablePropertyIds.Add(property.Prop_Id);
                    }
                    /*
                    if (!entry.Value.EligablePropertyIds.Contains(property.Prop_Id) && property.collections.Any(c => c.Id == entry.Value.Id))
                    {
                        entry.Value.EligablePropertyIds.Add(property.Prop_Id);
                    }
                    */
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

            foreach (Up2LandProperty property in userProperties)
            {
                this.Properties.Add(property.Prop_Id, property);
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
                        entry.Value.MonthlyUpx += HelperFunctions.GetMonthlyUpxByMintAndBoost(this.Properties[id].Mint_Price, entry.Value.Boost);
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

            foreach (KeyValuePair<long, Up2LandProperty> entry in this.Properties)
            {
                if (this.SlottedPropertyIds.Contains(entry.Value.Prop_Id) || ignorePropertyIds.Contains(entry.Value.Prop_Id))
                {
                    continue;
                }

                if (!cityProCollections.Any(c => c.Key == entry.Value.City_Id))
                {
                    StandardCollectionBuilder newCityCollection = new StandardCollectionBuilder
                    {
                        Id = entry.Value.City_Id,
                        PropIds = new List<long>(),
                        MonthlyUpx = 0
                    };
                    cityProCollections.Add(newCityCollection.Id, newCityCollection);
                }

                if (cityProCollections[entry.Value.City_Id].PropIds.Count < 5)
                {
                    cityProCollections[entry.Value.City_Id].PropIds.Add(entry.Value.Prop_Id);
                    cityProCollections[entry.Value.City_Id].MonthlyUpx += HelperFunctions.GetMonthlyUpxByMintAndBoost(entry.Value.Mint_Price, Consts.CityProBoost);
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

            foreach (KeyValuePair<long, Up2LandProperty> entry in this.Properties)
            {
                if (this.SlottedPropertyIds.Contains(entry.Value.Prop_Id) || ignorePropertyIds.Contains(entry.Value.Prop_Id) || entry.Value.Street_Id == null || entry.Value.Street_Id <= 0)
                {
                    continue;
                }

                if (!kingOfTheStreetCollections.Any(c => c.Key == entry.Value.Street_Id))
                {
                    StandardCollectionBuilder newStreetCollection = new StandardCollectionBuilder
                    {
                        Id = entry.Value.Street_Id,
                        PropIds = new List<long>(),
                        MonthlyUpx = 0
                    };
                    kingOfTheStreetCollections.Add(newStreetCollection.Id, newStreetCollection);
                }

                if (kingOfTheStreetCollections[entry.Value.Street_Id].PropIds.Count < 3)
                {
                    kingOfTheStreetCollections[entry.Value.Street_Id].PropIds.Add(entry.Value.Prop_Id);
                    kingOfTheStreetCollections[entry.Value.Street_Id].MonthlyUpx += HelperFunctions.GetMonthlyUpxByMintAndBoost(entry.Value.Mint_Price, Consts.KingOfTheStreetBoost);
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

            Up2LandProperty largestUnslotted = this.Properties.Where(p => !this.SlottedPropertyIds.Contains(p.Value.Prop_Id))?.FirstOrDefault().Value;

            if (largestUnslotted != null)
            {
                newbie.SlottedPropertyIds.Add(largestUnslotted.Prop_Id);
                newbie.MonthlyUpx += HelperFunctions.GetMonthlyUpxByMintAndBoost(largestUnslotted.Mint_Price, 1.1);
                this.SlottedPropertyIds.Add(largestUnslotted.Prop_Id);
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
                if(!HelperFunctions.IsCollectionStd(collection))
                {
                    string collectionCity = this.AllCities[this.Properties[collection.SlottedPropertyIds[0]].City_Id];
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
                    outputStrings.Add(string.Format("     {0}", this.Properties[propertyId].Full_Address));
                    Console.WriteLine("     {0}", this.Properties[propertyId].Full_Address);
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

            // DEBUG
            //outputStrings = WriteOutAllPropertyData(outputStrings);
            // END DEBUG

            return outputStrings;
        }

        private List<string> WriteOutAllPropertyData(List<string> outputStrings)
        {
            List<Up2LandProperty> allProps = this.Properties.OrderBy(p => p.Value.City_Id).Select(p => p.Value).ToList<Up2LandProperty>();
            double stotal = 0;
            double vTotal = 0;
            foreach (Up2LandProperty prop in allProps)
            {
                if (this.SlottedPropertyIds.Contains(prop.Prop_Id))
                {
                    foreach (KeyValuePair<int, Collection> entry in this.FilledCollections)
                    {
                        if (entry.Value.SlottedPropertyIds.Contains(prop.Prop_Id))
                        {
                            outputStrings.Add(string.Format("{0} - {1} - {2:N2}", prop.City_Id, prop.Full_Address, prop.Mint_Price * entry.Value.Boost * Consts.ReturnRate / 12.0));
                            stotal += prop.Mint_Price * entry.Value.Boost * Consts.ReturnRate / 12.0;
                            break;
                        }
                    }
                }
                else
                {
                    outputStrings.Add(string.Format("{0} - {1} - {2:N2}", prop.City_Id, prop.Full_Address, prop.Mint_Price * Consts.ReturnRate /12.0));
                    vTotal += prop.Mint_Price * Consts.ReturnRate / 12.0;
                }
            }
            outputStrings.Add("");
            outputStrings.Add(string.Format("sTotal - {0}", stotal));
            outputStrings.Add(string.Format("vTotal - {0}", vTotal));

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
            return this.Properties.Sum(p => p.Value.Mint_Price) * Consts.ReturnRate / 12.0;
        }

        private double CalcualteMonthylUpx()
        {
            double total = 0;

            total += this.FilledCollections.Sum(c => c.Value.MonthlyUpx);
            total += this.Properties.Where(p => !this.SlottedPropertyIds.Contains(p.Value.Prop_Id)).Sum(p => p.Value.Mint_Price) * Consts.ReturnRate / 12.0;

            return total;
        }
    }
}
