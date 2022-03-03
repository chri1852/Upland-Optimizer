using System.Collections.Generic;
using System.Threading.Tasks;

namespace Upland.Interfaces.Processors
{
    public interface IInformationProcessor
    {
        List<string> GetCollectionInformation(string fileType);
        List<string> GetNeighborhoodInformation(string fileType);
        List<string> GetStreetInformation(string fileType);
        List<string> GetCityInformation(string fileType);
        Task<List<string>> GetPropertyInfo(string username, string fileType);
        List<string> SearchStreets(string name, string fileType);
        List<string> SearchNeighborhoods(string name, string fileType);
        List<string> SearchCollections(string name, string fileType);
        List<string> SearchProperties(int cityId, string address, string fileType);
        public List<string> CatchWhales();
        List<string> GetUnmintedProperties(string type, int Id, string propType, string fileType);
        List<string> GetAllProperties(string type, int Id, string fileType);
        Task<List<string>> GetAssetsByTypeAndUserName(string type, string userName, string fileType);
        List<string> GetSaleHistoryByType(string type, string identifier, string fileType);
        void ClearSalesCache();
        Task RebuildPropertyStructures();
        Task LoadMissingCityProperties(int cityId);
        Task ResetLockedPropsToLocked(int cityId);
        Task<List<string>> GetBuildingsUnderConstruction(int userLevel);
    }
}