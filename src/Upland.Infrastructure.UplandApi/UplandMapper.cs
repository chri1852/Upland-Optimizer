using System.Collections.Generic;
using System;
using Upland.Types;
using Upland.Types.UplandApiTypes;

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

            return collection;
        }

        public static List<Property> Map(List<UplandDistinctProperty> uplandDistinctProperties)
        {
            List<Property> properties = new List<Property>();

            foreach (UplandDistinctProperty property in uplandDistinctProperties)
            {
                properties.Add(Map(property));
            }

            return properties;
        }

        public static Property Map(UplandDistinctProperty udProperty)
        {
            Property property = new Property();

            property.Id = udProperty.Prop_Id;
            property.Address = udProperty.Full_Address;
            property.CityId = udProperty.City.Id;
            property.StreetId = udProperty.Street.Id;
            property.Size = udProperty.Area;
            property.MonthlyEarnings = udProperty.Yield_Per_Hour * 720;

            return property;
        }

        private static List<int> GetCityTags(UplandCollection uplandCollection)
        {
            List<int> ids = new List<int>();

            foreach(TagsAddressCommon item in uplandCollection.Tags_Address_Common)
            {
                int idResult = 0;
                if (int.TryParse(item.City, out idResult))
                {
                    ids.Add(idResult);
                }
            }

            return ids;
        }

        private static List<int> GetStreetTags(UplandCollection uplandCollection)
        {
            List<int> ids = new List<int>();

            foreach (TagsAddressCommon item in uplandCollection.Tags_Address_Common)
            {
                int idResult = 0;
                if (int.TryParse(item.Street, out idResult))
                {
                    ids.Add(idResult);
                }
            }

            return ids;
        }

        private static List<int> GetNeighborhoodTags(UplandCollection uplandCollection)
        {
            List<int> ids = new List<int>();

            foreach (TagsAddressCommon item in uplandCollection.Tags_Address_Common)
            {
                int idResult = 0;
                if (int.TryParse(item.Neighborhood, out idResult))
                {
                    ids.Add(idResult);
                }
            }

            return ids;
        }
    }
}
