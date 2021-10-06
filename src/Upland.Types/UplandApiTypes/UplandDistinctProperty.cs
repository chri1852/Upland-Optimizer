using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.UplandApiTypes
{
    public class UplandDistinctProperty
    {
        public long Prop_Id { get; set; }
        public string Full_Address { get; set; }
        public pCity City { get; set; }
        public pCity Street { get; set; }
        public pMarket on_market { get; set; }
        public double Yield_Per_Hour { get; set; }
        public int Area { get; set; }
    }

    public class pCity
    {
        public int Id { get; set; }
    }

    public class pMarket
    {
        public string token;
    }
}
