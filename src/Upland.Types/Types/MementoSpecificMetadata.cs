using System;
using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class MementoSpecificMetadata
    {
        public int LegitId { get; set; }
        public DateTime GameDate { get; set; }
        public string OpponentTeam { get; set; }
        public string HomeTeam { get; set; }
        public List<NFLPALegitStatObject> MainStats { get; set; }
        public List<NFLPALegitStatObject> AdditionalStats { get; set; }
    }
}
