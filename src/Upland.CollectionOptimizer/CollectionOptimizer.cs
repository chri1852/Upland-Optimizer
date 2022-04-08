using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Repositories;
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
        private ILocalDataManager LocalDataManager;
        private bool DebugMode;
        private IUplandApiRepository UplandApiRepository;

        private Stopwatch timer;

        private const string CityPro = "City Pro";
        private const string KingOfTheStreet = "King of the Street";
        private const int CityProId = 21;
        private const int KingOfTheStreetId = 1;

        public CollectionOptimizer(ILocalDataManager localDataManager, IUplandApiRepository uplandApiRepository)
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

            this.LocalDataManager = localDataManager;
            this.UplandApiRepository = uplandApiRepository;
            this.DebugMode = false;

            this.timer = new Stopwatch();
        }

        public async Task RunAutoOptimization(RegisteredUser registeredUser, OptimizerRunRequest runRequest)
        {
            if (runRequest.DebugRun)
            {
                await RunDebugOptimization(runRequest);
                return;
            }

            LocalDataManager.CreateOptimizationRun(
                new OptimizationRun
                {
                    RegisteredUserId = registeredUser.Id
                });
            OptimizationRun optimizationRun = LocalDataManager.GetLatestOptimizationRun(registeredUser.Id);

            try
            {
                await SetDataForOptimization(runRequest);
                RunOptimization(registeredUser.UplandUsername, runRequest.Level);
            }
            catch (Exception ex)
            {
                LocalDataManager.CreateErrorLog("CollectionOptimizer - RunAutoOptimization", ex.Message);
                LocalDataManager.SetOptimizationRunStatus(
                    new OptimizationRun
                    {
                        Id = optimizationRun.Id,
                        Status = Consts.RunStatusFailed,
                        Results = new byte[] { }
                    });
                return;
            }

            OptimizerResults results = BuildOptimizerResults(registeredUser.UplandUsername, runRequest.Level);

            LocalDataManager.SetOptimizationRunStatus(
                new OptimizationRun
                {
                    Id = optimizationRun.Id,
                    Status = Consts.RunStatusCompleted,
                    Results = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(results))
                }); ;

            if (!runRequest.AdminRun)
            {
                registeredUser.RunCount++;
                LocalDataManager.UpdateRegisteredUser(registeredUser);
            }
        }

        private async Task RunDebugOptimization(OptimizerRunRequest runRequest)
        {
            this.DebugMode = true;
            await SetDataForOptimization(runRequest);
            RunOptimization(runRequest.Username, runRequest.Level);

            OptimizerResults results = BuildOptimizerResults(runRequest.Username, runRequest.Level);

            if (this.DebugMode)
            {
                HelperFunctions.WriteCollecitonToConsole(this.FilledCollections, this.Properties, this.SlottedPropertyIds, this.UnfilledCollections, this.UnoptimizedCollections, this.MissingCollections);
            }
        }

        private OptimizerResults BuildOptimizerResults(string username, int qualityLevel)
        {
            List<Collection> collections = this.FilledCollections.OrderByDescending(c => c.Value.TotalMint).Select(c => c.Value).ToList();

            OptimizerResults results = new OptimizerResults();
            results.Username = username;
            results.RunDateTime = DateTime.UtcNow;
            results.QualityLevel = qualityLevel;
            results.TimeToRunTicks = this.timer.Elapsed.Ticks;
            results.OptimizedCollections = new List<OptimizerCollectionResult>();
            results.UnfilledCollections = new List<OptimizerCollectionResult>();
            results.UnoptimizedCollections = new List<OptimizerCollectionResult>();
            results.ExtraCollections = new List<OptimizerCollectionResult>();
            results.MissingCollections = new List<OptimizerCollectionResult>();

            results.BaseTotalIncome = HelperFunctions.CalculateBaseMonthlyUPX(Properties);
            results.BoostedTotalIncome = HelperFunctions.CalculateMonthlyUpx(FilledCollections, Properties, SlottedPropertyIds);

            // Used for determining Extra Collection Props
            List<long> allNonCityOrStandardSlottedPropIds = new List<long>();
            foreach (KeyValuePair<int, Collection> entry in FilledCollections)
            {
                if (!entry.Value.IsCityCollection && !Consts.StandardCollectionIds.Contains(entry.Value.Id))
                {
                    allNonCityOrStandardSlottedPropIds.AddRange(entry.Value.SlottedPropertyIds);
                }
            }

            foreach (Collection collection in collections)
            {
                OptimizerCollectionResult collectionResult = new OptimizerCollectionResult();
                collectionResult.City = Consts.Cities[this.Properties[collection.SlottedPropertyIds[0]].CityId];
                collectionResult.IsStandardCollection = Consts.StandardCollectionIds.Contains(collection.Id);

                collectionResult.Name = collection.Name;
                collectionResult.Category = Consts.CollectionCategories[collection.Category];
                collectionResult.Boost = collection.Boost;
                collectionResult.MissingProps = 0;
                collectionResult.Properties = new List<OptimizerCollectionProperty>();

                collectionResult.CategoryId = collection.Category;
                collectionResult.CityId = collectionResult.IsStandardCollection ? 0 : this.Properties[collection.SlottedPropertyIds[0]].CityId;

                foreach (long propertyId in collection.SlottedPropertyIds)
                {
                    OptimizerCollectionProperty property = new OptimizerCollectionProperty();
                    property.Address = this.Properties[propertyId].Address;
                    property.BaseIncome = this.Properties[propertyId].Mint * Consts.RateOfReturn / 12.0;

                    collectionResult.Properties.Add(property);
                }

                results.OptimizedCollections.Add(collectionResult);

                // Build the extra list
                if (!collectionResult.IsStandardCollection 
                    && !collection.IsCityCollection
                    && collection.EligablePropertyIds.Where(p => !allNonCityOrStandardSlottedPropIds.Contains(p)).ToList().Count > 0)
                {
                    OptimizerCollectionResult extraCollectionResult = new OptimizerCollectionResult();
                    extraCollectionResult.City = Consts.Cities[this.Properties[collection.SlottedPropertyIds[0]].CityId];
                    extraCollectionResult.IsStandardCollection = Consts.StandardCollectionIds.Contains(collection.Id);

                    extraCollectionResult.Name = collection.Name;
                    extraCollectionResult.Category = Consts.CollectionCategories[collection.Category];
                    extraCollectionResult.Boost = collection.Boost;
                    extraCollectionResult.MissingProps = collection.EligablePropertyIds.Count - collection.SlottedPropertyIds.Count;
                    extraCollectionResult.Properties = new List<OptimizerCollectionProperty>();

                    extraCollectionResult.CategoryId = collection.Category;
                    extraCollectionResult.CityId = extraCollectionResult.IsStandardCollection ? 0 : this.Properties[collection.SlottedPropertyIds[0]].CityId;

                    foreach (long propertyId in collection.EligablePropertyIds.Where(p => !allNonCityOrStandardSlottedPropIds.Contains(p)))
                    {
                        OptimizerCollectionProperty property = new OptimizerCollectionProperty();
                        property.Address = this.Properties[propertyId].Address;
                        property.BaseIncome = this.Properties[propertyId].Mint * Consts.RateOfReturn / 12.0;

                        extraCollectionResult.Properties.Add(property);
                    }
                    results.ExtraCollections.Add(extraCollectionResult);
                }
            }

            if (UnfilledCollections.Count > 0)
            {
                foreach (KeyValuePair<int, Collection> entry in this.UnfilledCollections)
                {
                    OptimizerCollectionResult collectionResult = new OptimizerCollectionResult();

                    collectionResult.IsStandardCollection = !entry.Value.CityId.HasValue;
                    collectionResult.City = entry.Value.CityId.HasValue ? Consts.Cities[entry.Value.CityId.Value] : "Standard";
                    collectionResult.Name = entry.Value.Name;
                    collectionResult.Category = Consts.CollectionCategories[entry.Value.Category];
                    collectionResult.Boost = entry.Value.Boost;
                    collectionResult.MissingProps = entry.Value.NumberOfProperties - entry.Value.EligablePropertyIds.Count;
                    collectionResult.Properties = new List<OptimizerCollectionProperty>();

                    collectionResult.CategoryId = entry.Value.Category;
                    collectionResult.CityId = entry.Value.CityId.HasValue ? entry.Value.CityId.Value : 0;

                    foreach (long propertyId in entry.Value.EligablePropertyIds)
                    {
                        OptimizerCollectionProperty property = new OptimizerCollectionProperty();
                        property.Address = this.Properties[propertyId].Address;
                        property.BaseIncome = this.Properties[propertyId].Mint * Consts.RateOfReturn / 12.0;

                        collectionResult.Properties.Add(property);
                    }

                    results.UnfilledCollections.Add(collectionResult);
                }
            }

            if (UnoptimizedCollections.Count > 0)
            {
                foreach (KeyValuePair<int, Collection> entry in this.UnoptimizedCollections)
                {
                    OptimizerCollectionResult collectionResult = new OptimizerCollectionResult();

                    collectionResult.IsStandardCollection = !entry.Value.CityId.HasValue;
                    collectionResult.City = entry.Value.CityId.HasValue ? Consts.Cities[entry.Value.CityId.Value] : "Standard";
                    collectionResult.Name = entry.Value.Name;
                    collectionResult.Category = Consts.CollectionCategories[entry.Value.Category];
                    collectionResult.Boost = entry.Value.Boost;
                    collectionResult.MissingProps = entry.Value.NumberOfProperties - entry.Value.EligablePropertyIds.Count;
                    collectionResult.Properties = new List<OptimizerCollectionProperty>();

                    collectionResult.CategoryId = entry.Value.Category;
                    collectionResult.CityId = entry.Value.CityId.HasValue ? entry.Value.CityId.Value : 0;

                    foreach (long propertyId in entry.Value.EligablePropertyIds)
                    {
                        OptimizerCollectionProperty property = new OptimizerCollectionProperty();
                        property.Address = this.Properties[propertyId].Address;
                        property.BaseIncome = this.Properties[propertyId].Mint * Consts.RateOfReturn / 12.0;

                        collectionResult.Properties.Add(property);
                    }

                    results.UnoptimizedCollections.Add(collectionResult);
                }
            }

            if (MissingCollections.Count > 0)
            {
                foreach (KeyValuePair<int, Collection> entry in this.MissingCollections)
                {
                    OptimizerCollectionResult collectionResult = new OptimizerCollectionResult();

                    collectionResult.IsStandardCollection = !entry.Value.CityId.HasValue;
                    collectionResult.City = entry.Value.CityId.HasValue ? Consts.Cities[entry.Value.CityId.Value] : "Standard";
                    collectionResult.Name = entry.Value.Name;
                    collectionResult.Category = Consts.CollectionCategories[entry.Value.Category];
                    collectionResult.Boost = entry.Value.Boost;
                    collectionResult.MissingProps = entry.Value.NumberOfProperties;
                    collectionResult.Properties = new List<OptimizerCollectionProperty>();

                    collectionResult.CategoryId = entry.Value.Category;
                    collectionResult.CityId = entry.Value.CityId.HasValue ? entry.Value.CityId.Value : 0;

                    results.MissingCollections.Add(collectionResult);
                }
            }

            results.UnfilledCollections = SortOptimizerResults(results.UnfilledCollections, "COLLECTION");
            results.UnoptimizedCollections = SortOptimizerResults(results.UnoptimizedCollections, "COLLECTION");
            results.ExtraCollections = SortOptimizerResults(results.ExtraCollections, "COLLECTION");
            results.MissingCollections = SortOptimizerResults(results.MissingCollections, "COLLECTION");

            return results;
        }

        private void CreateHypotheicalProperties(OptimizerRunRequest runRequest)
        {
            if (Consts.StandardCollectionIds.Contains(runRequest.WhatIfCollectionId))
            {
                return;
            }

            List<Property> properties = HelperFunctions.BuildHypotheicalProperties(this.Collections[runRequest.WhatIfCollectionId].CityId.Value, runRequest.WhatIfNumProperties, runRequest.WhatIfAverageMintUpx);
            int matchingCityCollectionId = this.Collections.Where(c => c.Value.IsCityCollection && c.Value.CityId == this.Collections[runRequest.WhatIfCollectionId].CityId.Value).First().Value.Id;

            foreach (Property property in properties)
            {
                this.Collections[runRequest.WhatIfCollectionId].MatchingPropertyIds.Add(property.Id);
                if (matchingCityCollectionId != runRequest.WhatIfCollectionId)
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

            foreach (KeyValuePair<int, Collection> entry in this.Collections.OrderByDescending(c => c.Value.TotalMint))
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
                foreach (KeyValuePair<int, Collection> entry in this.Collections.OrderByDescending(c => c.Value.TotalMint))
                {
                    if (conflictingCollections.Count() < qualityLevel)
                    {
                        conflictingCollections.Add(entry.Key, entry.Value.Clone());
                    }
                }
                return conflictingCollections;
            }

            // We now have one collection lets loop through all collections til we have all the conflicts.
            foreach (KeyValuePair<int, Collection> entry in this.Collections.OrderByDescending(c => c.Value.TotalMint))
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
            foreach (KeyValuePair<int, Collection> entry in this.Collections.OrderByDescending(c => c.Value.TotalMint))
            {
                // if its city pro or King of the street, or any eligable props on collection match eligable props on another collections
                if (!conflictingCollections.ContainsKey(entry.Value.Id)
                    && !Consts.StandardCollectionIds.Contains(entry.Value.Id)
                    && !entry.Value.IsCityCollection)  
                {
                    if (!conflictingCollections.Any(e => e.Value.IsCityCollection && e.Value.CityId == entry.Value.CityId)
                        && this.Collections.Any(e => e.Value.IsCityCollection && e.Value.CityId == entry.Value.CityId))
                    {
                        if (conflictingCollections.Count < qualityLevel - 1)
                        {
                            conflictingCollections.Add(entry.Key, entry.Value.Clone());

                            Collection cityCollection = this.Collections.First(e => e.Value.IsCityCollection && e.Value.CityId == entry.Value.CityId).Value;
                            conflictingCollections.Add(cityCollection.Id, cityCollection.Clone());
                        }
                    }
                    else if (conflictingCollections.Count < qualityLevel)
                    {
                        conflictingCollections.Add(entry.Key, entry.Value.Clone());
                    }
                }
            }
            
            return conflictingCollections;
        }

        private async Task SetDataForOptimization(OptimizerRunRequest runRequest)
        {
            LoadCollections();

            // Only build the hypotheticals if requested
            if (runRequest.WhatIfRun)
            {
                CreateHypotheicalProperties(runRequest);
            }

            // its an exclude run, lets remove the requested collections
            if (runRequest.ExcludeRun)
            {
                foreach (int excludeId in runRequest.ExcludeCollectionIds)
                {
                    this.Collections.Remove(excludeId);
                }
            }

            await LoadUserProperties(runRequest.Username);
            PopulatePropertiesAndCollections();

            BuildAllCityProCollections();
            BuildAllKingOfTheStreetCollections();

            this.Collections = RebuildCollections(this.Collections, new List<long>(), false);
        }

        private void RunOptimization(string username, int qualityLevel)
        {
            this.timer.Start();

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

            BuildBestNewbieCoollection();

            this.timer.Stop();
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
                double collectionMax = Math.Round(RecursiveMaxUpxFinder(copiedCollections, entry.Value, ignorePropertyIds));

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
                if (DoesCollectionConflictWithList(entry.Value, collections.Where(c => c.Key != entry.Value.Id)))
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
                return collection.TotalMint;
            }
            else
            {
                if (!AnyCollectionConflict(collections))
                {
                    return collections.Sum(c => c.Value.TotalMint);
                }
                List<long> copiedIgnorePropertyIds = new List<long>(ignorePropertyIds);
                Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(new Dictionary<int, Collection>(
                    collections.Where(c => c.Value.Id != collection.Id)
                ));
                copiedIgnorePropertyIds.AddRange(collection.SlottedPropertyIds);

                copiedCollections = RemoveIdsFromCollections(copiedCollections, collection.SlottedPropertyIds);
                copiedCollections = RebuildCollections(copiedCollections, copiedIgnorePropertyIds, false);

                // find the total mint 
                double doneMint = collections
                    .Where(c => !copiedCollections.ContainsKey(c.Key))
                    .Sum(c => c.Value.SlottedPropertyIds
                        .Where(e => !copiedIgnorePropertyIds.Contains(e))
                        .Sum(p => this.Properties[p].Mint));

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

                return collection.TotalMint + maxMonthly + doneMint;
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

            userProperties = userProperties.OrderByDescending(p => p.Mint).ToList();

            if (userProperties == null || userProperties.Count == 0)
            {
                throw new Exception("No Properties Found");
            }

            this.Properties = new Dictionary<long, Property>();
            foreach (Property property in userProperties)
            {
                this.Properties.Add(property.Id, property);
            }
        }

        private void LoadCollections()
        {
            this.Collections = new Dictionary<int, Collection>();

            List<Collection> collections = LocalDataManager.GetCollections();

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
                    .OrderByDescending(p => p.Value.Mint)
                    .Select(p => new KeyValuePair<long, double>(p.Value.Id, p.Value.Mint)).ToList();

                StandardCollectionBuilder newCityCollection = new StandardCollectionBuilder
                {
                    Id = cities.Key,
                    Props = props,
                    Boost = 1.4,
                    NumberOfProps = 5
                };
                this.CityProCollections.Add(newCityCollection);
            }
            this.CityProCollections = this.CityProCollections.OrderByDescending(c => c.TotalMint(new List<long>())).ToList();
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
                    .OrderByDescending(p => p.Value.Mint)
                    .Select(p => new KeyValuePair<long, double>(p.Value.Id, p.Value.Mint)).ToList();

                StandardCollectionBuilder newStreetCollection = new StandardCollectionBuilder
                {
                    Id = streets.Key,
                    Props = props,
                    Boost = 1.3,
                    NumberOfProps = 3
                };
                this.KingOfTheStreetCollections.Add(newStreetCollection);
            }
            this.KingOfTheStreetCollections = this.KingOfTheStreetCollections.OrderByDescending(c => c.TotalMint(new List<long>())).ToList();
        }

        private void RemovePropIdsFromStandardCollections(List<long> removeIds)
        {
            foreach (StandardCollectionBuilder collection in this.CityProCollections)
            {
                collection.Props.RemoveAll(p => removeIds.Contains(p.Key));
            }

            this.CityProCollections.RemoveAll(c => c.Props.Count < 5);
            this.CityProCollections = this.CityProCollections.OrderByDescending(c => c.TotalMint(new List<long>())).ToList();

            foreach (StandardCollectionBuilder collection in this.KingOfTheStreetCollections)
            {
                collection.Props.RemoveAll(p => removeIds.Contains(p.Key));
            }

            this.KingOfTheStreetCollections.RemoveAll(c => c.Props.Count < 3);
            this.KingOfTheStreetCollections = this.KingOfTheStreetCollections.OrderByDescending(c => c.TotalMint(new List<long>())).ToList();
        }

        private Dictionary<int, Collection> RebuildCollections(Dictionary<int, Collection> collections, List<long> ignorePropertyIds, bool placeRemovedInUnfilled)
        {
            Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(collections);
            List<int> removeCollectionIds = new List<int>();

            foreach (KeyValuePair<int, Collection> entry in copiedCollections)
            {
                entry.Value.SlottedPropertyIds = new List<long>();
                entry.Value.TotalMint = 0;

                foreach (long id in entry.Value.EligablePropertyIds)
                {
                    if (entry.Value.SlottedPropertyIds.Count < entry.Value.NumberOfProperties && !ignorePropertyIds.Contains(id))
                    {
                        entry.Value.SlottedPropertyIds.Add(id);
                        entry.Value.TotalMint += this.Properties[id].Mint * entry.Value.Boost;
                    }
                }

                if (entry.Value.SlottedPropertyIds.Count < entry.Value.NumberOfProperties
                    && entry.Value.Name != CityPro
                    && entry.Value.Name != KingOfTheStreet)
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
            if (!collections.Any(c => c.Value.Name == CityPro))
            {
                return collections;
            }

            Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(collections);

            if (this.CityProCollections.Any(c => c.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Count() >= 5))
            {
                StandardCollectionBuilder topCityProCollection =
                    this.CityProCollections.Where(c => c.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Count() >= 5).OrderByDescending(c => c.TotalMint(ignorePropertyIds)).First();
                copiedCollections[CityProId].SlottedPropertyIds = topCityProCollection.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Select(p => p.Key).Take(5).ToList();
                copiedCollections[CityProId].TotalMint = topCityProCollection.TotalMint(ignorePropertyIds);
            }
            else
            {
                copiedCollections.Remove(CityProId);
            }

            return copiedCollections;
        }

        private Dictionary<int, Collection> BuildBestKingOfTheStreetCollection(Dictionary<int, Collection> collections, List<long> ignorePropertyIds)
        {
            if (!collections.Any(c => c.Value.Name == KingOfTheStreet))
            {
                return collections;
            }

            Dictionary<int, Collection> copiedCollections = HelperFunctions.DeepCollectionClone(collections);

            if (this.KingOfTheStreetCollections.Any(c => c.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Count() >= 3))
            {
                StandardCollectionBuilder topKingOfTheStreetCollection =
                    this.KingOfTheStreetCollections.Where(c => c.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Count() >= 3).OrderByDescending(c => c.TotalMint(ignorePropertyIds)).First();
                copiedCollections[KingOfTheStreetId].SlottedPropertyIds = topKingOfTheStreetCollection.Props.Where(p => !ignorePropertyIds.Contains(p.Key)).Select(p => p.Key).Take(3).ToList();
                copiedCollections[KingOfTheStreetId].TotalMint = topKingOfTheStreetCollection.TotalMint(ignorePropertyIds);
            }
            else
            {
                copiedCollections.Remove(KingOfTheStreetId);
            }

            return copiedCollections;
        }

        private void BuildBestNewbieCoollection()
        {
            Collection newbie = new Collection
            {
                Id = 7,
                Name = "Newbie",
                Boost = 1.1,
                NumberOfProperties = 1,
                SlottedPropertyIds = new List<long>(),
                TotalMint = 0,
                Category = 1
            };

            Property largestUnslotted = this.Properties.Where(p => !this.SlottedPropertyIds.Contains(p.Value.Id))?.FirstOrDefault().Value;

            if (largestUnslotted != null)
            {
                newbie.SlottedPropertyIds.Add(largestUnslotted.Id);
                newbie.TotalMint += largestUnslotted.Mint * 1.1;
                this.SlottedPropertyIds.Add(largestUnslotted.Id);
                this.FilledCollections.Add(newbie.Id, newbie);
            }
        }

        public static string BuildTextOutput(OptimizerResults results, string sortBy = "UPX")
        {
            List<string> outputStrings = new List<string>();

            outputStrings.Add(string.Format("Collection Optimization Report - {0}", results.RunDateTime.ToString("MM-dd-yyyy")));
            outputStrings.Add("-------------------------------------------");
            outputStrings.Add(string.Format("Ran for {0} at Quality Level {1}", results.Username, results.QualityLevel));
            outputStrings.Add(string.Format("Run Time - {0}", new TimeSpan(results.TimeToRunTicks)));
            outputStrings.Add("");

            if (sortBy.ToUpper() != "UPX")
            {
                results.OptimizedCollections = SortOptimizerResults(results.OptimizedCollections, sortBy);
            }

            foreach (OptimizerCollectionResult collection in results.OptimizedCollections)
            {
                outputStrings.Add(string.Format("{0} - {1} - {2:N2} --> {3:N2}", collection.City, collection.Name, collection.Properties.Sum(p => p.BaseIncome), collection.Properties.Sum(p => p.BaseIncome) * collection.Boost));

                foreach (OptimizerCollectionProperty property in collection.Properties)
                {
                    outputStrings.Add(string.Format("     {0}", property.Address));
                }
                outputStrings.Add("");
            }

            string baseMonthlyUpx = string.Format("{0:N2}", results.BaseTotalIncome);
            string totalMonthlyUpx = string.Format("{0:N2}", results.BoostedTotalIncome);

            outputStrings.Add("");
            outputStrings.Add(string.Format("Base Monthly UPX...........: {0}", baseMonthlyUpx));
            outputStrings.Add(string.Format("Total Monthly UPX..........: {0}", totalMonthlyUpx));

            if (results.UnfilledCollections.Count > 0 || results.UnoptimizedCollections.Count > 0) // || MissingCollections.Count > 0)
            {
                outputStrings.Add("");

                if (results.UnfilledCollections.Count > 0)
                {
                    outputStrings.Add("");
                    outputStrings.Add(string.Format("Unfilled Collections"));
                    foreach (OptimizerCollectionResult collection in results.UnfilledCollections)
                    {
                        outputStrings.Add(string.Format("     {0} - {1} - Missing Props {2}", collection.City, collection.Name, collection.MissingProps));
                    }
                }

                if (results.UnoptimizedCollections.Count > 0)
                {
                    outputStrings.Add("");
                    outputStrings.Add("Unoptimized Collections");
                    foreach (OptimizerCollectionResult collection in results.UnoptimizedCollections)
                    {
                        outputStrings.Add(string.Format("     {0} - {1} - Missing Props {2}", collection.City, collection.Name, collection.MissingProps));
                    }
                }
            }

            return string.Join(Environment.NewLine, outputStrings);
        }

        public static string BuildCsvOutput(OptimizerResults results, string sortBy = "UPX")
        {
            List<string> outputStrings = new List<string>();

            outputStrings.Add("City,Collection,Category,Boost,Address,BaseEarnings,BoostedEarnings");

            if (sortBy.ToUpper() != "UPX")
            {
                results.OptimizedCollections = SortOptimizerResults(results.OptimizedCollections, sortBy);
            }

            foreach (OptimizerCollectionResult collection in results.OptimizedCollections)
            {
                foreach (OptimizerCollectionProperty property in collection.Properties)
                {
                    outputStrings.Add(string.Format("{0},{1},{2},{3},{4},{5},{6}",
                        collection.City,
                        collection.Name,
                        collection.Category,
                        collection.Boost,
                        property.Address,
                        property.BaseIncome,
                        property.BaseIncome * collection.Boost));
                }
            }

            return string.Join(Environment.NewLine, outputStrings);
        }

        public static List<OptimizerCollectionResult> SortOptimizerResults(List<OptimizerCollectionResult> results, string sortBy)
        {
            if (sortBy.ToUpper() == "COLLECTION")
            {
                return results.OrderBy(r => r.CityId)
                    .ThenBy(r => r.CategoryId)
                    .ThenBy(r => r.Boost)
                    .ToList();
            }

            return results;
        }
    }
}
