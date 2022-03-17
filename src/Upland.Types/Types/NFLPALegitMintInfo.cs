using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class NFLPALegitMintInfo
    {
        // Essential Only
        public bool IsVariant { get; set; }

        // Memento Only
        public NFLPALegitMintInfoStats Stats { get; set; }

        public int FanPoints { get; set; }
    }

    public class NFLPALegitMintInfoStats
    {
        public List<NFLPALegitStatObject> MainStats { get; set; }
        public List<NFLPALegitStatObject> AdditionalStats { get; set; }
    }

    public class NFLPALegitMintInfoScene
    {
        // Memento Only
        public DateTime GameDate { get; set; }
        public string PlayerHeadshotUrl { get; set; }
        public string OpponentTeamName { get; set; }
        public string HomeTeamName { get; set; }

        public string PlayerFullName { get; set; }
        public string PlayerTitle { get; set; }
        public string PlayerTeamName { get; set; }
        public string NFTCategory { get; set; }
        public string SceneUrl { get; set; }
        public string BackgroundImagePortraitUrl { get; set; }
        public string BackgroundImageLandscapeUrl { get; set; }
    }
}
