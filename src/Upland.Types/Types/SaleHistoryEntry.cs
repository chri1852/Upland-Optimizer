using System;

namespace Upland.Types.Types
{
    public class SaleHistoryEntry
    {
        public int? Id { get; set; }
        public DateTime DateTime { get; set; }
        public string? SellerEOS { get; set; }
        public string? BuyerEOS { get; set; }
        public long PropId { get; set; }
        public double? Amount { get; set; }
        public double? AmountFiat { get; set; }
        public long? OfferPropId { get; set; }
        public bool Offer { get; set; }
        public bool Accepted { get; set; }
    }
}
