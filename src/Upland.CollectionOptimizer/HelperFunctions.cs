using System;
using System.Collections.Generic;
using System.Linq;
using Upland.Types;

namespace Upland.CollectionOptimizer
{
    public static class HelperFunctions
    {
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
