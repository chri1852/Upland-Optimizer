using System;
using System.Collections.Generic;
using System.Linq;
using Upland.Types;

namespace Upland.CollectionOptimizer
{
    public static class HelperFunctions
    {
        public static double GetMonthlyUpxByMintAndBoost(double mintPrice, double boost)
        {
            return mintPrice * Consts.ReturnRate / 12 * boost;
        }

        public static bool IsCollectionStd(Collection collection)
        {
            return collection.Name == Consts.CityPro || collection.Name == Consts.KingOfTheStreet || collection.Name == Consts.Newbie;
        }

        public static int GetHighestCollectionMonthlyUpx(Dictionary<int, Collection> collections)
        {
            return collections.OrderByDescending(c => c.Value.MonthlyUpx).First().Key;
        }

        public static Dictionary<int, Collection> DeepCollectionClone(Dictionary<int, Collection> collections)
        {
            Dictionary<int, Collection> clonedCollections = new Dictionary<int, Collection>();

            foreach(KeyValuePair<int, Collection> entry in collections)
            {
                clonedCollections.Add(entry.Key, entry.Value.Clone());
            }

            return clonedCollections;
        }
    }
}
