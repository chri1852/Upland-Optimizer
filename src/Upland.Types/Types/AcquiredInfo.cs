using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class AcquiredInfo
    {
        public long PropertyId { get; set; }
        public bool Minted { get; set; }
        public DateTime? AcquiredDateTime { get; set; }
    }
}
