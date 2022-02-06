﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.Types.Types;

namespace Upland.Interfaces.Processors
{
    public interface IWebProcessor
    {
        Task<UserProfile> GetWebUIProfile(string uplandUsername);

        List<CachedForSaleProperty> GetForSaleProps(WebForSaleFilters filters, bool noPaging);

        List<CachedUnmintedProperty> GetUnmintedProperties(WebForSaleFilters filters, bool noPaging);

        List<CachedSaleHistoryEntry> GetSaleHistoryEntries(WebSaleHistoryFilters filters, bool noPaging);

        List<string> ConvertListCachedForSalePropertyToCSV(List<CachedForSaleProperty> cachedForSaleProperties);

        List<string> ConvertListCachedUnmintedPropertyToCSV(List<CachedUnmintedProperty> cachedUnmintedProperties);

        List<string> ConvertListCachedSaleHistoryEntriesToCSV(List<CachedSaleHistoryEntry> cachedSaleHistoryEntries);
    }
}