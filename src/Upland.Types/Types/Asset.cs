using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class Asset
    {
        public int DGoodId { get; set; }
        public string DisplayName { get; set; }
        public int Mint { get; set; }
        public int CurrentSupply { get; set; }
        public int MaxSupply { get; set; }
        public string Link { get; set; }
    }
}
