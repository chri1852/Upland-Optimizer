using System;

namespace Upland.Types.Types
{
    public class NFTSaleData
    {
        public int Id { get; set; }
        public int DGoodId { get; set; }
        public string SellerEOS { get; set; }
        public string BuyerEOS { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountFiat { get; set; }
        public DateTime DateTime { get; set; }
    }
}
