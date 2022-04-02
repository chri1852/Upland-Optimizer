using System;

namespace Upland.Types.Types
{
    public class SparkStaking
    {
        public int Id { get; set; }
        public int DGoodId { get; set; }
        public string EOSAccount { get; set; }
        public decimal Amount { get; set; }
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }
    }
}
