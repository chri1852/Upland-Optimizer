using System;

namespace Upland.Types.Types
{
    public class WebSparkHistory
    {
        public int Id { get; set; }
        public int DGoodId { get; set; }
        public long PropertyId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public decimal Amount { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
        public double SparkHours { get; set; }
        public bool Manufacturing { get; set; }
    }
}
