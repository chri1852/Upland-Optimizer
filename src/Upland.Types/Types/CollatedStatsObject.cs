namespace Upland.Types.Types
{
    public class CollatedStatsObject
    {
        public int Id { get; set; }
        public int TotalProps { get; set; }
        public int LockedProps { get; set; }
        public int UnlockedNonFSAProps { get; set; }
        public int UnlockedFSAProps { get; set; }
        public int ForSaleProps { get; set; }
        public int OwnedProps { get; set; }
        public double PercentMinted { get; set; }
        public double PercentNonFSAMinted { get; set; }
    }
}
