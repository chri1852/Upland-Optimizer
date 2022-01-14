using System;

namespace Upland.Types.Types
{
    public class AppraisalRun
    {
        public int Id { get; set; }
        public int RegisteredUserId { get; set; }
        public DateTime RequestedDateTime { get; set; }
        public byte[] Results { get; set; }
    }
}