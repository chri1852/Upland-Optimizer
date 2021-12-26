using System.Collections.Generic;
using System;
using Upland.Types;
using Upland.Types.UplandApiTypes;
using System.Linq;
using Upland.Types.Types;

namespace Upland.Infrastructure.UplandApi
{
    public static class UplandMapper
    {
        public static List<Collection> Map(List<UplandCollection> uplandCollections)
        {
            List<Collection> collections = new List<Collection>();
             
            foreach(UplandCollection uplandCollection in uplandCollections)
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
            collection.MonthlyUpx = 0;
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
            property.MonthlyEarnings = udProperty.Yield_Per_Hour.HasValue ? udProperty.Yield_Per_Hour.Value  * 720 : 0;
            property.NeighborhoodId = null;
            property.Latitude = udProperty.CenterLat;
            property.Longitude = udProperty.CenterLng;
            property.Status = udProperty.status;
            property.FSA = udProperty.labels.fsa_allow;
            property.Owner = udProperty.owner;

            return property;
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

            legit.PlayerName = asset.Metadata.PlayerFullNameUppercase;
            legit.Position = asset.Metadata.PlayerPosition;
            legit.LegitType = asset.Metadata.ModelType;
            legit.Year = asset.Metadata.Season;
            legit.FanPoints = asset.Metadata.FanPoints;

            legit.DGoodId = asset.DGoodId;
            legit.DisplayName = asset.Metadata.DisplayName;
            legit.Mint = asset.Mint;
            legit.CurrentSupply = asset.Stat.CurrentSupply;
            legit.MaxSupply = asset.Stat.MaxSupply;
            legit.Link = @"https://play.upland.me/legit-preview/" + asset.DGoodId;

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

            decoration.DGoodId = asset.DGoodId;
            decoration.DisplayName = asset.Metadata.DisplayName;
            decoration.Mint = asset.SerialNumber;
            decoration.CurrentSupply = asset.Stat.CurrentSupply;
            decoration.MaxSupply = asset.Stat.MaxSupply;
            decoration.Link = @"https://play.upland.me/nft-3d/decoration/" + asset.DGoodId;

            return decoration;
        }

        public static BlockExplorer MapBlockExplorer(UplandAsset asset)
        {
            BlockExplorer blockExplorer = new BlockExplorer();

            blockExplorer.Description = asset.Description;

            blockExplorer.DGoodId = asset.DGoodId;
            blockExplorer.DisplayName = asset.Name;
            blockExplorer.Mint = asset.SerialNumber;
            blockExplorer.MaxSupply = asset.TotalCount;
            blockExplorer.Link = @"https://play.upland.me/nft/block_explorer/" + asset.OwnerUsername + "/" + asset.DGoodId;

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
