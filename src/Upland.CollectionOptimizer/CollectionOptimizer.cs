using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.CollectionOptimizer
{
    public class CollectionOptimizer
    {
        private Dictionary<long, Property> Properties;
        private Dictionary<int, Collection> Collections;
        private List<long> SlottedPropertyIds;
        private Dictionary<int, Collection> FilledCollections;
        private Dictionary<int, Collection> UnfilledCollections;
        private List<StandardCollectionBuilder> CityProCollections;
        private List<StandardCollectionBuilder> KingOfTheStreetCollections;
        private Dictionary<int, Collection> UnoptimizedCollections;
        private Dictionary<int, Collection> MissingCollections;
        private LocalDataManager LocalDataManager;
        private bool DebugMode;

        public CollectionOptimizer()
        {
            this.Properties = new Dictionary<long, Property>();
            this.Collections = new Dictionary<int, Collection>();
            this.SlottedPropertyIds = new List<long>();
            this.FilledCollections = new Dictionary<int, Collection>();
    
            this.CityProCollections = new List<StandardCollectionBuilder>();
            this.KingOfTheStreetCollections = new List<StandardCollectionBuilder>();

            this.UnfilledCollections = new Dictionary<int, Collection>();
            this.UnoptimizedCollections = new Dictionary<int, Collection>();
            this.MissingCollections = new Dictionary<int, Collection>();

            this.LocalDataManager = new LocalDataManager();
            this.DebugMode = false;
        }

        public async Task RunDebugOptimization(string username, int qualityLevel, int collectionId = -1, int numberOfProperties = -1, double averageMonthlyUpx = -1)
        {
            this.DebugMode = true;
            await SetDataForOptimization(username, collectionId, numberOfProperties, averageMonthlyUpx);
            string results = RunOptimization(username, qualityLevel);
        }

        public async Task RunAutoOptimization(RegisteredUser registeredUser, int qualityLevel, int collectionId = -1, int numberOfProperties = -1, double averageMonthlyUpx = -1)
        {
            string results = "";
            LocalDataManager.CreateOptimizationRun(
                new OptimizationRun
                {
                    DiscordUserId = registeredUser.DiscordUserId
                });
            OptimizationRun optimizationRun = LocalDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);

            try
            {
                await SetDataForOptimization(registeredUser.UplandUsername, collectionId, numberOfProperties, averageMonthlyUpx);
                results = RunOptimization(registeredUser.UplandUsername, qualityLevel);
            }
            catch
            {
                LocalDataManager.SetOptimizationRunStatus(
                    new OptimizationRun
                    {
                        Id = optimizationRun.Id,
                        Status = Consts.RunStatusFailed,
                        Results = new byte[] { }
                    });
                return;
            }

            LocalDataManager.SetOptimizationRunStatus(
                new OptimizationRun
                {
                    Id = optimizationRun.Id,
                    Status = Consts.RunStatusCompleted,
                    Results = Encoding.UTF8.GetBytes(results)
                });
            LocalDataManager.IncreaseRegisteredUserRunCount(registeredUser.DiscordUserId);
        }
        
        private void CreateHypotheicalProperties(int collectionId, int numberOfProperties, double averageMonthlyUpx)
        {
            if(Consts.StandardCollectionIds.Contains(collectionId))
            {
                return;
            }

            List<Property> properties = HelperFunctions.BuildHypotheicalProperties(this.Collections[collectionId].CityId.Value, numberOfProperties, averageMonthlyUpx);
            int matchingCityCollectionId = this.Collections.Where(c => c.Value.IsCityCollection && c.Value.CityId == this.Collections[collectionId].CityId.Value).First().Value.Id;

            foreach (Property property in properties)
            {
                this.Collections[collectionId].MatchingPropertyIds.Add(property.Id);
                if (matchingCityCollectionId != collectionId)
                {
                    this.Collections[matchingCityCollectionId].MatchingPropertyIds.Add(property.Id);
                }
                this.Properties.Add(property.Id, property);
            }
        }

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
                // Don't use Standard or City Collections as the first collection
                if (Consts.StandardCollectionIds.Contains(entry.Value.Id) || entry.Value.IsCityCollection)
                {
                    continue;
                }

                firstCollection = entry.Value.Id;
                conflictingCollections.Add(entry.Value.Id, entry.Value.Clone());
                break;
            }
            
            // If the first collection is 0 all thats left is standard and city collections
            if (firstCollection == 0)
            {
                foreach (KeyValuePair<int, Collection> entry in this.Collections.OrderByDescending(c => c.Value.MonthlyUpx))
                {
                    if (conflictingCollections.Count() < qualityLevel)
                    {
                        conflictingCollections.Add(entry.Key, entry.Value.Clone());
                    }
                }
                return conflictingCollections;
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

            // Now lets top off the conflicts with the chonkers
            foreach (KeyValuePair<int, Collection> entry in this.Collections.OrderByDescending(c => c.Value.MonthlyUpx))
            {
                // if its city pro or King of the street, or any eligable props on collection match eligable props on another collections
                if (!conflictingCollections.ContainsKey(entry.Value.Id)
                    && conflictingCollections.Count < qualityLevel
                    && !Consts.StandardCollectionIds.Contains(entry.Value.Id)
                    && !entry.Value.IsCityCollection)
                {
                    conflictingCollections.Add(entry.Key, entry.Value.Clone());
                }
            }

            return conflictingCollections;
        }

        private async Task SetDataForOptimization(string username, int collectionId = -1, int numberOfProperties = -1, double averageMonthlyUpx = -1)
        {
            LoadCollections();

            // Only build the hypotheticals if requested
            if (collectionId != -1 && numberOfProperties != -1 && averageMonthlyUpx != -1)
            {
                CreateHypotheicalProperties(collectionId, numberOfProperties, averageMonthlyUpx);
            }

            await LoadUserProperties(username);
            PopulatePropertiesAndCollections();

            BuildAllCityProCollections();
            BuildAllKingOfTheStreetCollections();

            this.Collections = RebuildCollections(this.Collections, new List<long>(), false);
        }

        private string RunOptimization(string username, int qualityLevel)
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (this.Collections.Count > 0)
            {
                Dictionary<int, Collection> conflictingCollections = GetConflictingCollections(qualityLevel);
                
                int collectionToSlotId = RecursionWrapper(conflictingCollections);

                if (this.DebugMode)
                {
                    HelperFunctions.WriteCollectionFinishTime(this.Collections[collectionToSlotId]);
                }

                SetFilledCollection(collectionToSlotId);
            }

            timer.Stop();

            BuildBestNewbieCoollection();

            List<string> outputStrings = BuildOutput(timer, username, qualityLevel);

            if (this.DebugMode)
            {
                HelperFunctions.WriteCollecitonToConsole(this.FilledCollections, this.Properties, this.SlottedPropertyIds, this.UnfilledCollections, this.UnoptimizedCollections, this.MissingCollections);
            }

            return string.Join(Environment.NewLine, outputStrings);
        }

        private int RecursionWrapper(Dictionary<int, Collection> conflictingCollections)
        {
            double maxMonthly = 0;
            int maxCollectionId = -1;
            bool isMaxCityCollection = false;
            List<long> ignorePropertyIds = new List<long>();

            foreach (KeyValuePair<int, Collection> entry in conflictingCollections)
            {
                Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(conflictingCollections);
                double collectionMax = RecursiveMaxUpxFinder(copiedCollections, entry.Value, ignorePropertyIds);

                if (collectionMax > maxMonthly
                    || (collectionMax == maxMonthly && (Consts.StandardCollectionIds.Contains(maxCollectionId) || isMaxCityCollection)))
                {
                    maxMonthly = collectionMax;
                    maxCollectionId = entry.Value.Id;
                    isMaxCityCollection = entry.Value.IsCityCollection;
                }
            }

            return maxCollectionId;
        }

        private bool DoesCollectionConflictWithList(Collection collection, IEnumerable<KeyValuePair<int, Collection>> conflictingList)
        {
            return Consts.StandardCollectionIds.Contains(collection.Id) ||
                conflictingList.Any(e => e.Value.EligablePropertyIds.Any(p => collection.EligablePropertyIds.Contains(p)));
        }

        private bool AnyCollectionConflict(Dictionary<int, Collection> collections)
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
                if(!AnyCollectionConflict(collections))
                {
                    return collections.Sum(c => c.Value.MonthlyUpx);
                }
                List<long> copiedIgnorePropertyIds = new List<long>(ignorePropertyIds);
                Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(new Dictionary<int, Collection>(
                    collections.Where(c => c.Value.Id != collection.Id)
                ));
                copiedIgnorePropertyIds.AddRange(collection.SlottedPropertyIds);

                copiedCollections = RemoveIdsFromCollections(copiedCollections, collection.SlottedPropertyIds);
                copiedCollections = RebuildCollections(copiedCollections, copiedIgnorePropertyIds, false);

                double maxMonthly = 0;
                int maxId = -1;
                bool isMaxCityCollection = false;
                foreach (KeyValuePair<int, Collection> entry in copiedCollections)
                {
                    double newMax = RecursiveMaxUpxFinder(copiedCollections, entry.Value, copiedIgnorePropertyIds);
                    if (newMax > maxMonthly
                        || (newMax == maxMonthly && (Consts.StandardCollectionIds.Contains(maxId) || isMaxCityCollection)))
                    {
                        maxMonthly = newMax;
                        maxId = entry.Value.Id;
                        isMaxCityCollection = entry.Value.IsCityCollection;
                    }
                }

                return collection.MonthlyUpx + maxMonthly;
            }
        }

        private async Task LoadUserProperties(string username)
        {
            List<Property> userProperties = await this.LocalDataManager.GetPropertysByUsername(username);

            // Loads Hypothetical properties if any
            foreach (KeyValuePair<long, Property> entry in this.Properties)
            {
                userProperties.Add(entry.Value);
            }

            userProperties = userProperties.OrderByDescending(p => p.MonthlyEarnings).ToList();

            this.Properties = new Dictionary<long, Property>();
            foreach (Property property in userProperties)
            {
                this.Properties.Add(property.Id, property);
            }
        }

        private void LoadCollections()
        {
            this.Collections = new Dictionary<int, Collection>();

            LocalDataManager dataManager = new LocalDataManager();
            List<Collection> collections = dataManager.GetCollections();

            foreach (Collection collection in collections)
            {
                this.Collections.Add(collection.Id, collection);
            }
        }

        private void PopulatePropertiesAndCollections()
        {
            this.FilledCollections = new Dictionary<int, Collection>();
            this.SlottedPropertyIds = new List<long>();
            this.UnfilledCollections = new Dictionary<int, Collection>();
            this.CityProCollections = new List<StandardCollectionBuilder>();
            this.KingOfTheStreetCollections = new List<StandardCollectionBuilder>();
            this.UnoptimizedCollections = new Dictionary<int, Collection>();
            this.MissingCollections = new Dictionary<int, Collection>();

            foreach (KeyValuePair<int, Collection> entry in this.Collections)
            {
                foreach (KeyValuePair<long, Property> property in this.Properties)
                {
                    if ((!Consts.StandardCollectionIds.Contains(entry.Value.Id) && !entry.Value.IsCityCollection && entry.Value.MatchingPropertyIds.Contains(property.Value.Id))
                        || (entry.Value.IsCityCollection && entry.Value.CityId == property.Value.CityId))
                    {
                        entry.Value.EligablePropertyIds.Add(property.Value.Id);
                    }
                }
            }

            foreach (KeyValuePair<int, Collection> entry in this.Collections)
            {
                if (entry.Value.EligablePropertyIds.Count < entry.Value.NumberOfProperties && entry.Value.EligablePropertyIds.Count > 0)
                {
                    this.UnoptimizedCollections.Add(entry.Key, entry.Value.Clone());
                }

                if (entry.Value.EligablePropertyIds.Count == 0 && !Consts.StandardCollectionIds.Contains(entry.Value.Id))
                {
                    this.MissingCollections.Add(entry.Key, entry.Value.Clone());
                }
            }
        }
        
        private void BuildAllCityProCollections()
        {
            IEnumerable<KeyValuePair<int, int>> cityList = this.Properties
                .GroupBy(p => p.Value.CityId)
                .Where(g => g.Count() >= 5)
                .Select(group => new KeyValuePair<int, int>(group.Key, group.Count()));

            foreach (KeyValuePair<int, int> cities in cityList)
            {
                List<KeyValuePair<long, double>> props = this.Properties
                        .Where(p => p.Value.CityId == cities.Key)
                        .OrderByDescending(p => p.Value.MonthlyEarnings)
                        .Select(p => new KeyValuePair<long, double>(p.Value.Id, p.Value.MonthlyEarnings)).ToList();

                StandardCollectionBuilder newCityCollection = new StandardCollectionBuilder
                {
                    Id = cities.Key,
                    Props = props,
                    Boost = 1.4,
                    NumberOfProps = 5
                };
                this.CityProCollections.Add(newCityCollection);
            }
            this.CityProCollections = this.CityProCollections.OrderByDescending(c => c.MonthlyUpx(new List<long>())).ToList();
        }

        private void BuildAllKingOfTheStreetCollections()
        {
            IEnumerable<KeyValuePair<int, int>> streetList = this.Properties
                .GroupBy(p => p.Value.StreetId)
                .Where(g => g.Count() >= 3)
                .Select(group => new KeyValuePair<int, int>(group.Key, group.Count()));

            foreach (KeyValuePair<int, int> streets in streetList)
            {
                List<KeyValuePair<long, double>> props = this.Properties
                        .Where(p => p.Value.StreetId == streets.Key)
                        .OrderByDescending(p => p.Value.MonthlyEarnings)
                        .Select(p => new KeyValuePair<long, double>(p.Value.Id, p.Value.MonthlyEarnings)).ToList();

                StandardCollectionBuilder newStreetCollection = new StandardCollectionBuilder
                {
                    Id = streets.Key,
                    Props = props,
                    Boost = 1.3,
                    NumberOfProps = 3
                };
                this.KingOfTheStreetCollections.Add(newStreetCollection);
            }
            this.KingOfTheStreetCollections = this.KingOfTheStreetCollections.OrderByDescending(c => c.MonthlyUpx(new List<long>())).ToList();
        }

        private void RemovePropIdsFromStandardCollections(List<long> removeIds)
        { 
            foreach (StandardCollectionBuilder collection in this.CityProCollections)
            {
                collection.Props.RemoveAll(p => removeIds.Contains(p.Key));
            }

            this.CityProCollections.RemoveAll(c => c.Props.Count < 5);
            this.CityProCollections = this.CityProCollections.OrderByDescending(c => c.MonthlyUpx(new List<long>())).ToList();

            foreach (StandardCollectionBuilder collection in this.KingOfTheStreetCollections)
            {
                collection.Props.RemoveAll(p => removeIds.Contains(p.Key));
            }

            this.KingOfTheStreetCollections.RemoveAll(c => c.Props.Count < 3);
            this.KingOfTheStreetCollections = this.KingOfTheStreetCollections.OrderByDescending(c => c.MonthlyUpx(new List<long>())).ToList();
        }

        private Dictionary<int, Collection> RebuildCollections(Dictionary<int, Collection> collections, List<long> ignorePropertyIds, bool placeRemovedInUnfilled)
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
                if (placeRemovedInUnfilled)
                {
                    this.UnfilledCollections.Add(id, copiedCollections[id]);
                }
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
            this.UnfilledCollections = RemoveIdsFromCollections(this.UnfilledCollections, this.FilledCollections[collectionId].SlottedPropertyIds);
            RemovePropIdsFromStandardCollections(this.FilledCollections[collectionId].SlottedPropertyIds);
            this.Collections = RebuildCollections(this.Collections, new List<long>(), true);
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
            
            if (this.CityProCollections.Any(c => c.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Count() >= 5))
            {
                StandardCollectionBuilder topCityProCollection =
                    this.CityProCollections.Where(c => c.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Count() >= 5).OrderByDescending(c => c.MonthlyUpx(ignorePropertyIds)).First();
                copiedCollections[Consts.CityProId].SlottedPropertyIds = topCityProCollection.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Select(p => p.Key).Take(5).ToList();
                copiedCollections[Consts.CityProId].MonthlyUpx = topCityProCollection.MonthlyUpx(ignorePropertyIds);
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

            if (this.KingOfTheStreetCollections.Any(c => c.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Count() >= 3))
            {
                StandardCollectionBuilder topKingOfTheStreetCollection =
                    this.KingOfTheStreetCollections.Where(c => c.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Count() >= 3).OrderByDescending(c => c.MonthlyUpx(ignorePropertyIds)).First();
                copiedCollections[Consts.KingOfTheStreetId].SlottedPropertyIds = topKingOfTheStreetCollection.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Select(p => p.Key).Take(3).ToList();
                copiedCollections[Consts.KingOfTheStreetId].MonthlyUpx = topKingOfTheStreetCollection.MonthlyUpx(ignorePropertyIds);
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

        private List<string> BuildOutput(Stopwatch timer, string username, int qualityLevel)
        {
            int TotalCollectionRewards = 0;
            List<string> outputStrings = new List<string>();
            List<Collection> collections = this.FilledCollections.OrderByDescending(c => c.Value.MonthlyUpx).Select(c => c.Value).ToList();

            outputStrings.Add(string.Format("Collection Optimization Report - {0}", DateTime.Now.ToString("MM-dd-yyyy")));
            outputStrings.Add("-------------------------------------------");
            outputStrings.Add(string.Format("Ran for {0} at Quality Level {1}", username, qualityLevel));
            outputStrings.Add(string.Format("Run Time - {0}", timer.Elapsed));
            outputStrings.Add("");

            foreach (Collection collection in collections)
            {
                if(!Consts.StandardCollectionIds.Contains(collection.Id))
                {
                    string collectionCity = Consts.Cities[this.Properties[collection.SlottedPropertyIds[0]].CityId];
                    outputStrings.Add(string.Format("{0} - {1} - {2:N2} --> {3:N2}", collectionCity, collection.Name, collection.MonthlyUpx / collection.Boost, collection.MonthlyUpx));
                    TotalCollectionRewards += collection.Reward;
                }
                else
                {
                    outputStrings.Add(string.Format("Standard - {0} - {1:N2} --> {2:N2}", collection.Name, collection.MonthlyUpx / collection.Boost, collection.MonthlyUpx));
                }

                foreach (long propertyId in collection.SlottedPropertyIds)
                {
                    outputStrings.Add(string.Format("     {0}", this.Properties[propertyId].Address));
                }
                outputStrings.Add("");
            }

            string baseMonthlyUpx = string.Format("{0:N2}", HelperFunctions.CalculateBaseMonthlyUPX(Properties));
            string totalMonthlyUpx = string.Format("{0:N2}", HelperFunctions.CalculateMonthlyUpx(FilledCollections, Properties, SlottedPropertyIds));

            outputStrings.Add("");
            outputStrings.Add(string.Format("Base Monthly UPX...........: {0}", baseMonthlyUpx));
            outputStrings.Add(string.Format("Total Monthly UPX..........: {0}", totalMonthlyUpx));

            if (UnfilledCollections.Count > 0 || UnoptimizedCollections.Count > 0) // || MissingCollections.Count > 0)
            {
                outputStrings.Add("");

                if (UnfilledCollections.Count > 0)
                {
                    outputStrings.Add("");
                    outputStrings.Add(string.Format("Unfilled Collections"));
                    foreach (KeyValuePair<int, Collection> entry in this.UnfilledCollections)
                    {
                        string collectionCity = entry.Value.CityId.HasValue ? Consts.Cities[entry.Value.CityId.Value] : "Standard";
                        outputStrings.Add(string.Format("     {0} - {1} - Missing Props {2}", collectionCity, entry.Value.Name, entry.Value.NumberOfProperties - entry.Value.EligablePropertyIds.Count));
                    }
                }

                if (UnoptimizedCollections.Count > 0)
                {
                    outputStrings.Add("");
                    outputStrings.Add("Unoptimized Collections");
                    foreach (KeyValuePair<int, Collection> entry in this.UnoptimizedCollections)
                    {
                        string collectionCity = entry.Value.CityId.HasValue ? Consts.Cities[entry.Value.CityId.Value] : "Standard";
                        outputStrings.Add(string.Format("     {0} - {1} - Missing Props {2}", collectionCity, entry.Value.Name, entry.Value.NumberOfProperties - entry.Value.EligablePropertyIds.Count));
                    }
                }

                // Takes Up too much space on the report, can reenable later
                /*
                if (MissingCollections.Count > 0)
                {
                    outputStrings.Add("");
                    outputStrings.Add("Missing Collections");
                    foreach (KeyValuePair<int, Collection> entry in this.MissingCollections)
                    {
                        outputStrings.Add(string.Format("     {0} - Missing Props {1}", entry.Value.Name, entry.Value.NumberOfProperties));
                    }
                }
                */
            }

            return outputStrings;
        }
    }
}
