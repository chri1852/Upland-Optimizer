using System.Collections.Generic;
using System;
using Upland.Types;
using Upland.Types.UplandApiTypes;
using System.Linq;

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
            property.Size = udProperty.Area;
            property.MonthlyEarnings = udProperty.Yield_Per_Hour * 720;

            return property;
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
