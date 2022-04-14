using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.Types.Enums;
using Upland.Types.Types;

namespace Upland.Interfaces.Processors
{
    public interface IWebProcessor
    {
        string GetLatestAnnouncement();

        bool GetIsBlockchainUpdatesDisabled();

        Task<UserProfile> GetWebUIProfile(string uplandUsername);

        List<CachedForSaleProperty> GetForSaleProps(WebForSaleFilters filters, bool noPaging);

        List<CachedUnmintedProperty> GetUnmintedProperties(WebForSaleFilters filters, bool noPaging);

        List<CachedSaleHistoryEntry> GetSaleHistoryEntries(WebSaleHistoryFilters filters, bool noPaging);

        List<CollatedStatsObject> GetInfoByType(StatsTypes statsType);

        List<WebNFT> SearchNFTs(WebNFTFilters filters);

        List<WebNFTHistory> GetNFTHistory(int dGoodId);

        List<string> ConvertListCachedForSalePropertyToCSV(List<CachedForSaleProperty> cachedForSaleProperties);

        List<string> ConvertListCachedUnmintedPropertyToCSV(List<CachedUnmintedProperty> cachedUnmintedProperties);

        List<string> ConvertListCachedSaleHistoryEntriesToCSV(List<CachedSaleHistoryEntry> cachedSaleHistoryEntries);

        List<string> ConvertListWebNFTSToCSV(List<WebNFT> nfts, string category);

        List<UIPropertyHistory> GetPropertyHistory(long propertyId);
    }
}