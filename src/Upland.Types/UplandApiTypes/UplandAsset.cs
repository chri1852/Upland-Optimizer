using System;

namespace Upland.Types.UplandApiTypes
{
    public class UplandAsset
    {
        public int DGoodId { get; set; }
        public int Mint { get; set; }
        public int SerialNumber { get; set; }
        public string Category { get; set; }
        public string Owner { get; set; }
        public Metadata Metadata { get; set; }
        public Stat Stat { get; set; }
        public string OwnerUsername { get; set; }
        public bool IsLocked { get; set; }
        public string Description { get; set; }
        public int TotalCount { get; set; }
        public string Name { get; set; }
    }

    public class Stat
    {
        public int MaxSupply { get; set; }
        public int CurrentSupply { get; set; }
        public int IssuedSupply { get; set; }
        public DateTime MaxIssueWindow { get; set; }
    }

    public class Metadata
    {
        public string DisplayName { get; set; }
        public string Image { get; set; }
        public string TransactionId { get; set; }
        public string Username { get; set; }
        public string RarityLevel { get; set; }
        public string Subtitle { get; set; }
        public int DecorationId { get; set; }
        public int LegitId { get; set; }
        public string TeamName { get; set; }
    }
}
