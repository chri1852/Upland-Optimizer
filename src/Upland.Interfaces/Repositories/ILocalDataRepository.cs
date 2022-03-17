﻿using System;
using System.Collections.Generic;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Interfaces.Repositories
{
    public interface ILocalDataRepository
    {
        public void CreateCollection(Collection collection);
        void CreateCollectionProperties(int collectionId, List<long> propertyIds);
        void CreateErrorLog(string location, string message);
        List<CachedForSaleProperty> GetCachedForSaleProperties(int cityId);
        List<CachedSaleHistoryEntry> GetCachedSaleHistoryEntries(WebSaleHistoryFilters filters);
        List<Tuple<string, double>> CatchWhales();
        List<Tuple<int, long>> GetCollectionPropertyTable();
        List<long> GetCollectionPropertyIds(int collectionId);
        List<Collection> GetCollections();
        List<StatsObject> GetCityStats();
        List<StatsObject> GetNeighborhoodStats();
        List<StatsObject> GetStreetStats();
        List<StatsObject> GetCollectionStats();
        List<AcquiredInfo> GetAcquiredInfoByUser(string uplandUsername);
        List<PropertyAppraisalData> GetPreviousSalesAppraisalData();
        List<PropertyAppraisalData> GetCurrentFloorAppraisalData();
        List<PropertyAppraisalData> GetCurrentMarkupFloorAppraisalData();
        List<Tuple<string, double>> GetBuildingAppraisalData();
        void UpsertProperty(Property property);
        void UpsertEOSUser(EOSUser eosUser);
        void UpsertSaleHistory(SaleHistoryEntry saleHistory);
        Property GetProperty(long id);
        List<Property> GetProperties(List<long> propertyIds);
        List<Property> GetPropertiesByUplandUsername(string uplandUsername);
        List<Property> GetPropertiesByCollectionId(int collectionId);
        List<Property> GetPropertiesByCityId(int cityId);
        List<UplandForSaleProp> GetPropertiesForSale_City(int cityId, bool onlyBuildings);
        List<UplandForSaleProp> GetPropertiesForSale_Neighborhood(int neighborhoodId, bool onlyBuildings);
        List<UplandForSaleProp> GetPropertiesForSale_Street(int streetId, bool onlyBuildings);
        List<UplandForSaleProp> GetPropertiesForSale_Collection(int collectionId, bool onlyBuildings);
        List<UplandForSaleProp> GetPropertiesForSale_Seller(string uplandUsername, bool onlyBuildings);
        Property GetPropertyByCityIdAndAddress(int cityId, string address);
        List<PropertySearchEntry> SearchProperties(int cityId, string address);
        void CreateOptimizationRun(OptimizationRun optimizationRun);
        void CreateAppraisalRun(AppraisalRun appraisalRun);
        void CreateNeighborhood(Neighborhood neighborhood);
        void CreateHistoricalCityStatus(CollatedStatsObject statsObject);
        void CreateStreet(Street street);
        List<Neighborhood> GetNeighborhoods();
        List<Street> GetStreets();
        List<CollatedStatsObject> GetHistoricalCityStatusByCityId(int cityId);
        string GetConfigurationValue(string name);
        DateTime GetLastHistoricalCityStatusDate();
        DateTime GetLastSaleHistoryDateTime();
        List<SaleHistoryEntry> GetRawSaleHistoryByPropertyId(long propertyId);
        void SetOptimizationRunStatus(OptimizationRun optimizationRun);
        OptimizationRun GetLatestOptimizationRun(int id);
        AppraisalRun GetLatestAppraisalRun(int id);
        void CreateRegisteredUser(RegisteredUser registeredUser);
        void UpdateRegisteredUser(RegisteredUser registeredUser);
        RegisteredUser GetRegisteredUser(decimal discordUserId);
        RegisteredUser GetRegisteredUserByUplandUsername(string uplandUsername);
        EOSUser GetUplandUsernameByEOSAccount(string eosAccount);
        EOSUser GetEOSAccountByUplandUsername(string uplandUsername);
        List<Tuple<decimal, string, string>> GetRegisteredUsersEOSAccounts();
        List<SaleHistoryQueryEntry> GetSaleHistoryByCityId(int cityId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByNeighborhoodId(int neighborhoodId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByCollectionId(int collectionId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByStreetId(int streetId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByPropertyId(long propertyId);
        List<SaleHistoryQueryEntry> GetSaleHistoryByBuyerUsername(string buyerUsername);
        List<SaleHistoryQueryEntry> GetSaleHistoryBySellerUsername(string sellerUsername);
        void DeleteRegisteredUser(int id); 
        void DeleteOptimizerRuns(int id);
        void DeleteAppraisalRuns(int id);
        void UpdateSaleHistoryVistorToUplander(string oldEOS, string newEOS);
        void DeleteSaleHistoryById(int id);
        void DeleteEOSUser(string eosAccount);
        void DeleteSaleHistoryByPropertyId(long propertyId);
        void DeleteSaleHistoryByBuyerEOS(string eosAccount);
        void UpsertConfigurationValue(string name, string value);
        void CreatePropertyStructure(PropertyStructure propertyStructure);
        void TruncatePropertyStructure();
        List<PropertyStructure> GetPropertyStructures();
        void UpsertSparkStaking(SparkStaking sparkStaking);
        List<SparkStaking> GetSparkStakingByEOSUserId(int eosUserId);
        void UpsertNft(NFT nft);
        void UpsertNftMetadata(NFTMetadata nftMetadata);
        void UpsertNftHistory(NFTHistory nftHistory);
        NFT GetNftByDGoodId(int dGoodId);
        NFTMetadata GetNftMetadataById(int id);
        NFTMetadata GetNftMetadataByNameAndCategory(string name, string category);
        List<NFTHistory> GetNftHistoryByDGoodId(string dGoodId);
    }
}   