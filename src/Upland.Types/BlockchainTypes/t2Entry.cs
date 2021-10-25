using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.BlockchainTypes
{
    public class t2Entry
    {
        public long a34 { get; set; } //PropId
        public string a35 { get; set; } //Seller EOS
        public string f3 { get; set; } //Fiat String 3.50 FIAT
        public string f4 { get; set; } // Upx String 3.5 UPX
        public bool f21 { get; set; } // F21, 22, 23 are allow fiat/token/prop offers
        public bool f22 { get; set; }
        public bool f23 { get; set; }
    }
}
