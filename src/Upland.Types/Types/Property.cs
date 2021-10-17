using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Upland.Types
{
    public class Property : IEquatable<Property>
    {
        public long Id { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public int StreetId { get; set; }
        public int Size { get; set; }
        public double MonthlyEarnings { get; set; }
        public int? NeighborhoodId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool Equals(Property other)
        {
            return other.Id == this.Id;
        }
    }
}
