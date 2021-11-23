using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class t3Entry // Offer Table
    {
        public int f15 { get; set; }          // OfferId
        public string a35 { get; set; }       // Offering UserId
        public long f14 { get; set; }         // Target PropId
        public List<string> f25 { get; set; } // Offer ["asset|uint64", "propId"]
        public string f31 { get; set; }       // Offeree
    }
}
