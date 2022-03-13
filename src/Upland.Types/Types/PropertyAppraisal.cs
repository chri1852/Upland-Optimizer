using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class PropertyAppraisal
    {
        public Property Property { get; set; }
        public double UPX_Lower { get; set; }
        public double UPX_Mid { get; set; }
        public double UPX_Upper { get; set; }
        public List<string> Notes { get; set; }
        public List<PropertyAppraisalFigure> Figures { get; set; }
    }

    public class PropertyAppraisalFigure
    {
        public string Type { get; set; }
        public double Value { get; set; }

        public PropertyAppraisalFigure() : this(null, 0) { }
        public PropertyAppraisalFigure(string type, decimal value)
        {
            this.Type = type;
            this.Value = (double)value;
        }
    }
}
