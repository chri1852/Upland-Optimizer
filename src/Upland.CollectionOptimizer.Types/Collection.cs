using System;

namespace Upland.CollectionOptimizer.Types
{
    public class Collection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Category { get; set; }
        public double Boost { get; set; }
        public int NumberOfProperties { get; set; }
        public long[] SlottedPropertyIds { get; set; }
        public long[] EligablePropertyIds { get; set; }
        public string Description { get; set; }
        public double MonthlyUpx { get; set; }
        public int[] CityIds { get; set; }
        public int[] StreetIds { get; set; }
        public int[] NeighborhoodIds { get; set; }
        public long[] MatchingPropertyIds { get; set; }
    }

}
