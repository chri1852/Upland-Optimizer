using System;

namespace Upland.Types.Types
{
    public class SaleHistoryQueryEntry
    {
        public DateTime DateTime { get; set; }
        public string Seller { get; set; }
        public string Buyer { get; set; }
        public bool Offer { get; set; }
        public int CityId { get; set; }
        public string Address { get; set; }
        public double Mint { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public double Markup { get; set; }
    }
}
