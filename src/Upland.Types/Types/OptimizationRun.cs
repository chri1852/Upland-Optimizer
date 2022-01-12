using System;

namespace Upland.Types.Types
{
    public class OptimizationRun
    {
        public int Id { get; set; }
        public int RegisteredUserId { get; set; }
        public DateTime RequestedDateTime { get; set; }
        public byte[] Results { get; set; }
        public string Status { get; set; }
    }
}
