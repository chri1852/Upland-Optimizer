using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class UserProfile
    {
        public string Username { get; set; }
        public string EOSAccount { get; set; }

        public string AvatarLink { get; set; }
        public string AvatarColor { get; set; }

        public string Rank { get; set; }
        public double Networth { get; set; }
        public bool Jailed { get; set; }

        public List<UserProfileCollection> Collections { get; set; }
        public List<UserProfileBadge> Badges { get; set; }
        public List<UserProfileProperty> Properties { get; set; }

        // Only for registeredUsers
        public bool RegisteredUser { get; set; }
        public bool Supporter { get; set; }
        public int RegisteredUserId { get; set; }
        public int RunCount { get; set; }
        public int MaxRuns { get; set; }
        public int UPXToSupporter { get; set; }
        public int UPXToNextRun { get; set; }
    }

    public class UserProfileCollection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
        public int? CityId { get; set; }
    }

    public class UserProfileBadge
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Image { get; set; }
    }

    public class UserProfileProperty
    {
        public long PropertyId { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Neighborhood { get; set; }
        public int Size { get; set; }
        public double Mint { get; set; }
        public string Status { get; set; }
        public string Building { get; set; }
        public List<int> CollectionIds { get; set; }

        public bool Minted { get; set; }
        public DateTime? AcquiredOn { get; set; }
    }
}
