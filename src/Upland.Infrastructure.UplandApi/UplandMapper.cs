using System;
using System.Collections.Generic;
using System.Linq;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.Infrastructure.UplandApi
{
    public static class UplandMapper
    {
        public static List<Collection> Map(List<UplandCollection> uplandCollections)
        {
            List<Collection> collections = new List<Collection>();

            foreach (UplandCollection uplandCollection in uplandCollections)
            {
                collections.Add(Map(uplandCollection));
            }

            return collections;
        }

        public static Collection Map(UplandCollection uplandCollection)
        {
            Collection collection = new Collection();

            collection.Id = uplandCollection.Id;
            collection.Name = uplandCollection.Name;
            collection.Category = uplandCollection.Category;
            collection.Boost = uplandCollection.Yield_Boost;
            collection.NumberOfProperties = uplandCollection.Amount;
            collection.SlottedPropertyIds = new List<long>();
            collection.EligablePropertyIds = new List<long>();
            collection.Description = uplandCollection.Requirements;
            collection.TotalMint = 0;
            collection.MatchingPropertyIds = new List<long>();
            collection.Reward = uplandCollection.One_Time_Reward;
            collection.CityId = uplandCollection.City_Id;
            collection.IsCityCollection = IsCollectionCityCollection(uplandCollection);

            return collection;
        }

        public static List<Property> Map(List<UplandProperty> uplandDistinctProperties)
        {
            List<Property> properties = new List<Property>();

            foreach (UplandProperty property in uplandDistinctProperties)
            {
                properties.Add(Map(property));
            }

            return properties;
        }

        public static Property Map(UplandProperty udProperty)
        {
            Property property = new Property();

            property.Id = udProperty.Prop_Id;
            property.Address = udProperty.Full_Address;
            property.CityId = udProperty.City.Id;
            property.StreetId = udProperty.Street.Id;
            property.Size = udProperty.Area.HasValue ? udProperty.Area.Value : 0;
            property.Mint = udProperty.Yield_Per_Hour.HasValue ? udProperty.Yield_Per_Hour.Value * 8640 / Consts.RateOfReturn : 0;
            property.NeighborhoodId = null;
            property.Latitude = udProperty.CenterLat;
            property.Longitude = udProperty.CenterLng;
            property.Status = udProperty.status;
            property.FSA = udProperty.labels.fsa_allow;
            property.Owner = udProperty.owner;
            property.Boost = 1;

            return property;
        }

        public static List<UserProfile> Map(List<UplandUserProfile> uplandUserProfiles)
        {
            List<UserProfile> profiles = new List<UserProfile>();

            foreach (UplandUserProfile profile in uplandUserProfiles)
            {
                profiles.Add(Map(profile));
            }

            return profiles;
        }

        public static UserProfile Map(UplandUserProfile uplandUserProfile)
        {
            UserProfile profile = new UserProfile();

            profile.Username = uplandUserProfile.username;
            profile.EOSAccount = null;
            profile.AvatarLink = uplandUserProfile.avatar.Image;
            profile.AvatarColor = uplandUserProfile.color.Color;
            profile.Rank = uplandUserProfile.lvl.ToString();
            profile.Networth = uplandUserProfile.networth;
            profile.Jailed = uplandUserProfile.is_in_jail;

            profile.Collections = new List<UserProfileCollection>();
            profile.Badges = new List<UserProfileBadge>();
            profile.Properties = new List<UserProfileProperty>();

            foreach (UplandUserProfileCollection collection in uplandUserProfile.collections)
            {
                profile.Collections.Add(new UserProfileCollection
                {
                    Id = collection.Id,
                    Name = collection.Name,
                    Image = collection.Image_Thumbnail,
                    CityId = collection.City_Id
                });
            }

            foreach (UplandUserProfileBadge badge in uplandUserProfile.badges)
            {
                profile.Badges.Add(new UserProfileBadge
                {
                    Id = badge.Id,
                    Name = badge.Name,
                    Image = badge.Image
                });
            }

            foreach (UplandUserProfileProperty property in uplandUserProfile.properties)
            {
                profile.Properties.Add(new UserProfileProperty
                {
                    PropertyId = property.Property_Id
                });
            }

            return profile;
        }

        public static List<NFLPALegit> MapNFLPALegits(List<UplandAsset> assets)
        {
            List<NFLPALegit> legits = new List<NFLPALegit>();

            foreach (UplandAsset asset in assets)
            {
                legits.Add(MapNFLPALegit(asset));
            }

            return legits;
        }

        public static NFLPALegit MapNFLPALegit(UplandAsset asset)
        {
            NFLPALegit legit = new NFLPALegit();

            legit.TeamName = asset.Metadata.TeamName;
            legit.Category = asset.Category;

            if (asset.Metadata.PlayerFullNameUppercase == null)
            {
                if (asset.Metadata.DisplayName.Contains(" ESSENTIAL "))
                {
                    legit.PlayerName = asset.Metadata.DisplayName.Substring(5, asset.Metadata.DisplayName.Length - 5).Split(" ESSENTIAL ")[0];
                    legit.LegitType = asset.Metadata.DisplayName.Split(" ")[asset.Metadata.DisplayName.Split(" ").Length - 1].ToLower();
                    legit.Year = asset.Metadata.DisplayName.Split(" ")[0];
                }
                else if (asset.Metadata.DisplayName.Contains(" MEMENTO "))
                {
                    legit.PlayerName = asset.Metadata.DisplayName.Substring(5, asset.Metadata.DisplayName.Length - 5).Split(" MEMENTO ")[0];
                    legit.LegitType = asset.Metadata.DisplayName.Split(" ")[asset.Metadata.DisplayName.Split(" ").Length - 1].ToLower();
                    legit.Year = asset.Metadata.DisplayName.Split(" ")[0];
                }
                else if (asset.Metadata.DisplayName.Contains(" REPLICA "))
                {
                    legit.PlayerName = asset.Metadata.DisplayName.Substring(5, asset.Metadata.DisplayName.Length - 5).Split(" REPLICA ")[0];
                    legit.LegitType = asset.Metadata.DisplayName.Split(" ")[asset.Metadata.DisplayName.Split(" ").Length - 1].ToLower();
                    legit.Year = asset.Metadata.DisplayName.Split(" ")[0];
                }
            }
            else
            {
                legit.PlayerName = asset.Metadata.PlayerFullNameUppercase;
                legit.Position = asset.Metadata.PlayerPosition;
                legit.LegitType = asset.Metadata.ModelType;
                legit.Year = asset.Metadata.Season;
                legit.FanPoints = asset.Metadata.FanPoints;
            }

            legit.LegitId = asset.Metadata.LegitId;
            legit.DGoodId = asset.DGoodId;
            legit.DisplayName = asset.Metadata.DisplayName;
            legit.Mint = asset.Mint;
            legit.CurrentSupply = asset.Stat.CurrentSupply;
            legit.MaxSupply = asset.Stat.MaxSupply;
            legit.Link = @"https://play.upland.me/legit-preview/" + asset.DGoodId;
            legit.Image = @"https://static.upland.me/" + asset.Metadata.Image;

            return legit;
        }

        public static List<SpiritLegit> MapSpiritLegits(List<UplandAsset> assets)
        {
            List<SpiritLegit> legits = new List<SpiritLegit>();

            foreach (UplandAsset asset in assets)
            {
                legits.Add(MapSpiritLegit(asset));
            }

            return legits;
        }

        public static SpiritLegit MapSpiritLegit(UplandAsset asset)
        {
            SpiritLegit legit = new SpiritLegit();

            legit.Rarity = asset.Metadata.RarityLevel;

            legit.DGoodId = asset.DGoodId;
            legit.DisplayName = asset.Metadata.DisplayName;
            legit.Mint = asset.SerialNumber;
            legit.CurrentSupply = asset.Stat.CurrentSupply;
            legit.MaxSupply = asset.Stat.MaxSupply;
            legit.Link = @"https://play.upland.me/nft-3d/spirit/" + asset.DGoodId;
            legit.Image = @"https://static.upland.me/" + asset.Metadata.Image;

            return legit;
        }

        public static List<Decoration> MapDecorations(List<UplandAsset> assets)
        {
            List<Decoration> decorations = new List<Decoration>();

            foreach (UplandAsset asset in assets)
            {
                decorations.Add(MapDecoration(asset));
            }

            return decorations;
        }

        public static List<BlockExplorer> MapBlockExplorers(List<UplandAsset> assets)
        {
            List<BlockExplorer> blockExplorers = new List<BlockExplorer>();

            foreach (UplandAsset asset in assets)
            {
                blockExplorers.Add(MapBlockExplorer(asset));
            }

            return blockExplorers;
        }

        public static Decoration MapDecoration(UplandAsset asset)
        {
            Decoration decoration = new Decoration();

            decoration.Rarity = asset.Metadata.RarityLevel;
            decoration.Subtitle = asset.Metadata.Subtitle;
            decoration.DecorationId = asset.Metadata.DecorationId;

            decoration.DGoodId = asset.DGoodId;
            decoration.DisplayName = asset.Metadata.DisplayName;
            decoration.Mint = asset.SerialNumber;
            decoration.CurrentSupply = asset.Stat.CurrentSupply;
            decoration.MaxSupply = asset.Stat.MaxSupply;
            decoration.Link = @"https://play.upland.me/nft-3d/decoration/" + asset.DGoodId;
            decoration.Image = @"https://static.upland.me/" + asset.Metadata.Image;

            return decoration;
        }

        public static BlockExplorer MapBlockExplorer(UplandAsset asset)
        {
            BlockExplorer blockExplorer = new BlockExplorer();

            blockExplorer.Description = asset.Description;
            blockExplorer.SeriesId = asset.Series == null ? 0 : asset.Series.Id;
            blockExplorer.SeriesName = asset.Series == null ? "" : asset.Series.Name;
            blockExplorer.RarityLevel = asset.Metadata.RarityLevel;

            blockExplorer.DGoodId = asset.DGoodId;
            blockExplorer.DisplayName = asset.Name;
            blockExplorer.Mint = asset.SerialNumber;
            blockExplorer.MaxSupply = asset.TotalCount;
            blockExplorer.Link = @"https://play.upland.me/nft/block_explorer/nft-id/" + asset.DGoodId;
            blockExplorer.Image = asset.Image;

            return blockExplorer;
        }

        private static bool IsCollectionCityCollection(UplandCollection uplandCollection)
        {
            if (uplandCollection.Tags_Address_Common == null || uplandCollection.Tags_Address_Common.Count == 0)
            {
                return false;
            }

            if (uplandCollection.Tags_Address_Common.First()?.City != null && uplandCollection.Tags_Address_Common.First()?.City != "same")
            {
                return true;
            }

            return false;
        }
    }
}
