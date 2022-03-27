using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class WebNFT
    {
        public int DGoodId { get; set; }
        public string Image { get; set; }
        public string Link { get; set; }
        public int SerialNumber { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public int MaxSupply { get; set; }
        public int CurrentSupply { get; set; }

        // Spirithwln
        public string Rarity { get; set; }
        
        // Structornmt
        public string BuildingType { get; set; }

        // BlockExplorer
        public string Description { get; set; }
        public int SeriesId { get; set; }
        public string SeriesName { get; set; }

        // Essential
        public string Team { get; set; }
        public bool IsVariant { get; set; }
        public string Year { get; set; }
        public string Position { get; set; }
        public double FanPoints { get; set; }
        public string ModelType { get; set; }
        
        // Memento
        public DateTime GameDate { get; set; }
        public string Opponent { get; set; }
        public string HomeTeam { get; set; }
    }
}
