using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Interfaces.Repositories
{
    public interface IUplandApiRepository
    {
        Task<UplandUserProfile> GetProfileByUsername(string username);
        Task<List<UplandCollection>> GetCollections();
        Task<List<UplandCity>> GetCities();
        Task<List<Neighborhood>> GetNeighborhoods();
        Task<Street> GetStreet(int streetId);
        Task<List<UplandForSaleProp>> GetForSalePropsInArea(double north, double south, double east, double west);
        Task<List<UplandAuthProperty>> GetForSaleCollectionProperties(int collectionId);
        Task<List<UplandAuthProperty>> GetUnlockedNotForSaleCollectionProperties(int collectionId);
        Task<List<UplandCollection>> GetMatchingCollectionsByPropertyId(long propertyId);
        Task<List<UplandAuthProperty>> GetMatchingCollectionsOwned(int collectionId);
        Task<UplandProperty> GetPropertyById(long propertyId);
        Task<List<UplandAuthProperty>> GetPropertysByUsername(string username);
        Task<List<UplandProperty>> GetPropertiesByArea(double north, double west, double defaultStep);
        Task<List<UplandAsset>> GetNFLPALegitsByUserName(string username);
        Task<List<UplandAsset>> GetSpiritLegitsByUserName(string username);
        Task<List<UplandAsset>> GetDecorationsByUserName(string username);
        Task<List<UplandAsset>> GetBlockExplorersByUserName(string username);
        Task<UplandAsset> GetNFLPALegitsByDGoodId(int dGoodId);
        Task<UplandAsset> GetSpiritLegitsByDGoodId(int dGoodId);
        Task<UplandAsset> GetDecorationsByDGoodId(int dGoodId);
        Task<UplandAsset> GetBlockExplorersByDGoodId(int dGoodId);
        Task<NFLPALegitMintInfo> GetEssentialMintInfo(int legitId);
        Task<NFLPALegitMintInfo> GetMementoMintInfo(int legitId);
        Task<UplandExplorerCoordinates> GetExplorerCoordinates(string authToken = null);
        Task<UplandTreasureDirection> GetUplandTreasureDirection(long propId, string authToken = null);
        Task<bool> GetIsInMaintenance();
    }
}  
