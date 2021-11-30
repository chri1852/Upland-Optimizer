using System;

namespace Upland.Types.Types
{
    public class SparkStakingReport
    {
        public string Username { get; set; }
        public int Level { get; set; }
        public int CityId { get; set; }
        public string CityName { get; set; }
        public long PropertyId { get; set; }
        public string Address { get; set; }
        public double CurrentStakedSpark { get; set; }
        public double CurrentSparkProgress { get; set; }
        public double TotalSparkRequired { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime CurrentFinishDateTime { get; set; }
        public string ConstructionStatus { get; set; }
        public int NFTId { get; set; }
        public int ModelId { get; set; }
    }
}
