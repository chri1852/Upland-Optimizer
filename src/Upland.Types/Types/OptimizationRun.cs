using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class OptimizationRun
    {
        public int Id { get; set; }
        public decimal DiscordUserId { get; set; }
        public DateTime RequestedDateTime { get; set; }
        public byte[] Results { get; set; }
        public string Status { get; set; }
    }
}
