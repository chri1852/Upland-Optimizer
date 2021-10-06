using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.UplandApiTypes
{
    public class UplandProperty
    {
        public long Prop_Id { get; set; }
        public string Full_Address { get; set; }
        public LocationInfo City { get; set; }
        public LocationInfo Street { get; set; }
        public MarketInfo on_market { get; set; }
        public double Yield_Per_Hour { get; set; }
        public int Area { get; set; }
    }

    public class LocationInfo
    {
        public int Id { get; set; }
    }

    public class MarketInfo
    {
        public string token;
    }
}
