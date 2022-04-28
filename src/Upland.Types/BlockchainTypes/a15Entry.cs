using System;
using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class a15Entry // Property Table
    {
        public long a34 { get; set; }          // PropId
        //public string a35 { get; set; }        // owner eos
        //public string b4 { get; set; }         // Mint Price
       // public DateTime b5 { get; set; }       // Last Yield Time
        //public double b11 { get; set; }        // Collection Boost
        //public List<string> a31 { get; set; }  //Up2 Squares

        private decimal _b11;
        public decimal b11 
        { 
            get { return _b11;  }
            set { _b11 = Math.Round(value, 2); }
        }
    }
}
