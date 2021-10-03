using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.UplandApiTypes
{
    public class UplandProperty
    {
        public long Prop_Id { get; set; }
        public string Full_Address { get; set; }
        public string City_Id { get; set; }
        public string Street_Id { get; set; }
        public string Neighborhood_Id { get; set; }
    }
}
