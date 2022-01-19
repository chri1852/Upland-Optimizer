using System.Collections.Generic;

namespace Upland.Types.UplandApiTypes
{
    public class UplandUserProfile
    {
        public UplandUserProfileAvatar avatar { get; set; }
        public UplandUserProfileAvatarColor color { get; set; }
        public List<UplandUserProfileCollection> collections { get; set; }
        public double networth { get; set; }
        public List<UplandUserProfileProperty> properties { get; set; }
        public List<UplandUserProfileBadge> badges { get; set; }
        public bool is_in_jail { get; set; }
        public int lvl { get; set; }
        public string user_lvl_status { get; set; }

        public string username { get; set; }
        public List<long> propertyList { get; set; }
    }

    public class UplandUserProfileAvatar
    {
        public int Id { get; set; }
        public string Image { get; set; }
    }

    public class UplandUserProfileAvatarColor
    {
        public int Id { get; set; }
        public string Color { get; set; }
    }

    public class UplandUserProfileCollection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Image_Thumbnail { get; set; }
        public int? City_Id { get; set; }
    }

    public class UplandUserProfileProperty
    {
        public long Property_Id { get; set; }
    }

    public class UplandUserProfileBadge
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public string Help_Type { get; set; }
        public string User_Id { get; set; }
    }
}
