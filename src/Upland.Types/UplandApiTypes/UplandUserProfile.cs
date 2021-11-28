using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.UplandApiTypes
{
    public class UplandUserProfile
    {
        public int lvl { get; set; }
        public List<UplandUserProfileProperty> properties { get; set; }
        public double networth { get; set; }
        public bool is_in_jail { get; set; }

        public string username { get; set; }
        public List<long> propertyList { get; set; }
    }

    public class UplandUserProfileProperty
    {
        public long property_id { get; set; }
    }
}
