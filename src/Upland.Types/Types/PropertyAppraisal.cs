using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class PropertyAppraisal
    {
        public Property Property { get; set; }
        public double UPX_Lower { get; set; }
        public double UPX_Upper { get; set; }
        public List<string> Notes { get; set; }
    }
}
