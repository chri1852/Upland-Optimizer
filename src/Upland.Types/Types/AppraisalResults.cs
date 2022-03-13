using System;
using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class AppraisalResults
    {
        public string Username { get; set; }
        public DateTime RunDateTime { get; set; }

        public List<AppraisalProperty> Properties { get; set; }
    }

    public class AppraisalProperty
    {
        public string City { get; set; }
        public string Address { get; set; }
        public int Size { get; set; }
        public List<int> Collections { get; set; }
        public double Mint { get; set; }
        public double LowerValue { get; set; }
        public double MiddleValue { get; set; }
        public double UpperValue { get; set; }
        public string Note { get; set; }
        public List<PropertyAppraisalFigure> Figures { get; set; }
    }
}
