using System.Collections.Generic;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public interface ICachingProcessor
    {
        Dictionary<long, string> GetPropertyStructuresFromCache();
        List<CachedForSaleProperty> GetCityForSaleListFromCache(int cityId);
        List<CachedUnmintedProperty> GetCityUnmintedFromCache(int cityId);
        List<CollatedStatsObject> GetCityInfoFromCache();
        List<CollatedStatsObject> GetNeighborhoodInfoFromCache();
        List<CollatedStatsObject> GetStreetInfoFromCache();
        List<CollatedStatsObject> GetCollectionInfoFromCache();
        Dictionary<int, SpirithlwnMetadata> GetSpiritHlwnMetadataFromCache();
        Dictionary<int, StructornmtMetadata> GetStructornmtMetadataFromCache();
        Dictionary<int, BlockExplorerMetadata> GetBlockExplorerMetadataFromCache();
        Dictionary<int, EssentialMetadata> GetEssentialMetadataFromCache();
        Dictionary<int, MementoMetadata> GetMementoMetadataFromCache();
        Dictionary<int, StructureMetadata> GetStructureMetadataFromCache();
        bool GetIsBlockchainUpdatesDisabledFromCache();
        string GetLatestAnnouncemenFromCache();
        List<int> GetCollectionIdListForPropertyId(long propertyId);
        Dictionary<int, string> GetNeighborhoodsFromCache();
        Dictionary<int, int> GetCurrentNFTCountsFromCache();
    }
}