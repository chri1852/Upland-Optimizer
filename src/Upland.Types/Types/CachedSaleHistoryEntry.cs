using System;
using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class CachedSaleHistoryEntry
    {
        public DateTime TransactionDateTime { get; set; }
        public string Seller { get; set; }
        public string Buyer { get; set; }
        public double? Price { get; set; }
        public double? Markup { get; set; }
        public string Currency { get; set; }
        public bool Offer { get; set; }
        public CachedSaleHistoryEntryProperty Property { get; set; }
        public CachedSaleHistoryEntryProperty OfferProperty { get; set; }
    }

    public class CachedSaleHistoryEntryProperty
    {
        public long Id { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public int NeighborhoodId { get; set; }
        public double Mint { get; set; }
        public List<int> CollectionIds { get; set; }
    }
}