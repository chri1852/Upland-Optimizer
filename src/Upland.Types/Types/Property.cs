﻿using System;

namespace Upland.Types
{
    public class Property : IEquatable<Property>
    {
        public long Id { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public int StreetId { get; set; }
        public int Size { get; set; }
        public double Mint { get; set; }
        public int? NeighborhoodId { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Status { get; set; }
        public bool FSA { get; set; }
        public string Owner { get; set; }
        public DateTime? MintedOn { get; set; }
        public string MintedBy { get; set; }
        public decimal Boost { get; set; }

        public bool Equals(Property other)
        {
            return other.Id == this.Id;
        }
    }
}
