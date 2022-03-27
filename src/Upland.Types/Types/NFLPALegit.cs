using System;
using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class NFLPALegit : Asset
    {
        public int LegitId { get; set; }

        public string TeamName { get; set; }
        public string Category { get; set; }
        public string Year { get; set; }
        public string PlayerName { get; set; }
        public string LegitType { get; set; }
        public double FanPoints { get; set; }
        public string Position { get; set; }
    }
}
