﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Interfaces.Managers
{
    public interface ILocalDataManager
    {
        Task PopulateAllPropertiesInArea(double north, double south, double east, double west, int cityId);
        int GetNeighborhoodIdForProp(List<Neighborhood> neighborhoods, Property property);
        Task PopulateIndividualPropertyById(long propertyId, List<Neighborhood> neighborhoods);
        Task PopulateNeighborhoods();
        Task PopulateStreets();
        Task PopulateDatabaseCollectionInfo();
        void DetermineNeighborhoodIdsForCity(int cityId);
        bool IsPropertyInNeighborhood(Neighborhood neighborhood, Property property);
        List<CachedForSaleProperty> GetCachedForSaleProperties(int cityId);
        List<CachedUnmintedProperty> GetCachedUnmintedProperties(int cityId);
        List<CachedSaleHistoryEntry> GetCachedSaleHistoryEntries(WebSaleHistoryFilters filters);
        List<Tuple<int, long>> GetCollectionPropertyTable();
        Property GetProperty(long id);
        List<Property> GetProperties(List<long> ids);
        List<long> GetPropertyIdsByCollectionId(int collectionId);
        List<Property> GetPropertiesByUplandUsername(string uplandUsername);
        List<Property> GetPropertiesByCityId(int cityId);
        Property GetPropertyByCityIdAndAddress(int cityId, string address);
        List<Property> GetPropertiesByCollectionId(int collectionId);
        List<Collection> GetCollections();
        Task<List<Property>> GetPropertysByUsername(string username);
        List<Street> SearchStreets(string name);
        List<Neighborhood> SearchNeighborhoods(string name);
        List<Collection> SearchCollections(string name);
        List<PropertySearchEntry> SearchProperties(int cityId, string address);
        void SetHistoricalCityStats(DateTime timeStamp);
        List<CollatedStatsObject> GetCityStats();
        List<CollatedStatsObject> GetNeighborhoodStats();
        List<CollatedStatsObject> GetStreetStats();
        List<CollatedStatsObject> GetCollectionStats();
        List<PropertyAppraisalData> GetPreviousSalesAppraisalData();
        List<PropertyAppraisalData> GetCurrentFloorAppraisalData();
        List<Tuple<string, double>> GetBuildingAppraisalData();
        void CreateOptimizationRun(OptimizationRun optimizationRun);
        void CreateAppraisalRun(AppraisalRun appraisalRun);
        void CreateNeighborhood(Neighborhood neighborhood);
        void CreateStreet(Street street);
        void CreateErrorLog(string location, string message);
        List<Neighborhood> GetNeighborhoods();
        List<Street> GetStreets();
        void SetOptimizationRunStatus(OptimizationRun optimizationRun);
        OptimizationRun GetLatestOptimizationRun(int id);
        AppraisalRun GetLatestAppraisalRun(int id);
        RegisteredUser GetRegisteredUser(decimal discordUserId);
        RegisteredUser GetRegisteredUserByUplandUsername(string uplandUsername);
        List<SaleHistoryEntry> GetRawSaleHistoryByPropertyId(long propertyId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByCityId(int cityId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByNeighborhoodId(int neighborhoodId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByCollectionId(int collectionId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByStreetId(int streetId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByPropertyId(long propertyId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByBuyerUsername(string buyerUsername);
        List<SaleHistoryQueryEntry> GetSaleHistoryBySellerUsername(string sellerUsername);
        List<CollatedStatsObject> GetHistoricalCityStatsByCityId(int cityId);
        List<UplandForSaleProp> GetPropertiesForSale_City(int cityId, bool onlyBuildings);
        List<UplandForSaleProp> GetPropertiesForSale_Neighborhood(int neighborhoodId, bool onlyBuildings);
        List<UplandForSaleProp> GetPropertiesForSale_Street(int streetId, bool onlyBuildings);
        List<UplandForSaleProp> GetPropertiesForSale_Collection(int collectionId, bool onlyBuildings);
        List<UplandForSaleProp> GetPropertiesForSale_Seller(string uplandUsername, bool onlyBuildings);
        string GetConfigurationValue(string name);
        Tuple<string, string> GetUplandUsernameByEOSAccount(string eosAccount);
        string GetEOSAccountByUplandUsername(string uplandUsername);
        List<Tuple<decimal, string, string>> GetRegisteredUsersEOSAccounts();
        DateTime GetLastHistoricalCityStatusDate();
        DateTime GetLastSaleHistoryDateTime();
        void UpdateSaleHistoryVistorToUplander(string oldEOS, string newEOS);
        void DeleteSaleHistoryByBuyerEOSAccount(string eosAccount);
        void CreateRegisteredUser(RegisteredUser registeredUser);
        void UpdateRegisteredUser(RegisteredUser registeredUser);
        void DeleteRegisteredUser(int id);
        void DeleteEOSUser(string eosAccount);
        void DeleteSaleHistoryById(int id);
        void DeleteSaleHistoryByPropertyId(long propertyId);
        void DeleteOptimizerRuns(int id);
        void DeleteAppraisalRuns(int id);
        void TruncatePropertyStructure();
        void CreatePropertyStructure(PropertyStructure propertyStructure);
        List<PropertyStructure> GetPropertyStructures();
        void UpsertEOSUser(string eosAccount, string uplandUsername, DateTime joined);
        void UpsertSaleHistory(SaleHistoryEntry saleHistory);
        void UpsertConfigurationValue(string name, string value);
        void UpsertProperty(Property property);
    }
}