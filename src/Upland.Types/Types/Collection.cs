
using System.Collections.Generic;

namespace Upland.Types
{
    public class Collection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Category { get; set; }
        public double Boost { get; set; }
        public int NumberOfProperties { get; set; }
        public List<long> SlottedPropertyIds { get; set; }
        public List<long> EligablePropertyIds { get; set; }
        public string Description { get; set; }
        public double MonthlyUpx { get; set; }
        public List<int> CityIds { get; set; }
        public List<int> StreetIds { get; set; }
        public List<int> NeighborhoodIds { get; set; }
        public List<long> MatchingPropertyIds { get; set; }
        public int Reward { get; set; }
        public int? CityId { get; set; }

        public Collection Clone()
        {
            Collection Clone = new Collection();

            Clone.Id = this.Id;
            Clone.Name = this.Name;
            Clone.Category = this.Category;
            Clone.Boost = this.Boost;
            Clone.NumberOfProperties = this.NumberOfProperties;
            Clone.SlottedPropertyIds = this.SlottedPropertyIds == null ? new List<long>() : new List<long>(this.SlottedPropertyIds);
            Clone.EligablePropertyIds = this.EligablePropertyIds == null ? new List<long>() : new List<long>(this.EligablePropertyIds);
            Clone.Description = this.Description;
            Clone.MonthlyUpx = this.MonthlyUpx;
            Clone.CityIds = this.CityIds == null ? new List<int>() : new List<int>(this.CityIds);
            Clone.StreetIds = this.StreetIds == null ? new List<int>() : new List<int>(this.StreetIds);
            Clone.NeighborhoodIds = this.NeighborhoodIds == null ? new List<int>() : new List<int>(this.NeighborhoodIds);
            Clone.MatchingPropertyIds = this.MatchingPropertyIds == null ? new List<long>() : new List<long>(this.MatchingPropertyIds);
            Clone.Reward = this.Reward;
            Clone.CityId = this.CityId;

            return Clone;
        }
    }
}
