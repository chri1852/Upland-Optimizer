using System.Collections.Generic;

namespace Upland.Interfaces.Processors
{
    public interface IForSaleProcessor
    {
        List<string> GetCollectionPropertiesForSale(int collectionId, string orderBy, string currency, string fileType);
        List<string> GetNeighborhoodPropertiesForSale(int neighborhoodId, string orderBy, string currency, string fileType);
        List<string> GetBuildingPropertiesForSale(string type, int Id, string orderBy, string currency, string fileType);
        List<string> GetCityPropertiesForSale(int cityId, string orderBy, string currency, string fileType);
        List<string> GetStreetPropertiesForSale(int streetId, string orderBy, string currency, string fileType);
        List<string> GetUsernamePropertiesForSale(string uplandUsername, string orderBy, string currency, string fileType);
    }
}