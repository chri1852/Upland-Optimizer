using System.Collections.Generic;

namespace Upland.Types.UplandApiTypes
{
    public class UplandCollection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Amount { get; set; }
        public int Category { get; set; }
        public string Requirements { get; set; }
        public double Yield_Boost { get; set; }
        public int One_Time_Reward { get; set; }
        public bool Published { get; set; }
        public string Image { get; set; }
        public string Image_Thumbnail { get; set; }
        public List<string> Tags_Unique { get; set; }
        public List<string> Tags_Common { get; set; }
        public List<TagsAddressCommon> Tags_Address_Common { get; set; }
        public List<string> Tags_Address_Unique { get; set; }
        public int? City_Id { get; set; }
    }
}
