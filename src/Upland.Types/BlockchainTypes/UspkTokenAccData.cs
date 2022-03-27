using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class UspkTokenAccData
    {
        public string from { get; set; }
        public string to { get; set; }
        public string quantity { get; set; }
        public string memo { get; set; }

        public string p113 { get; set; }
        public List<string> p115 { get; set; }
        public string p45 { get; set; }
        public string p51 { get; set; }
        public string a54 { get; set; }
    }
}
