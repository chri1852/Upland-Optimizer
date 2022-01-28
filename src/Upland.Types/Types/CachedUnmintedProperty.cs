using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class CachedUnmintedProperty
    {
        public long Id { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public int NeighborhoodId { get; set; }
        public int StreetId { get; set; }
        public int Size { get; set; }
        public bool FSA { get; set; }
        public double Mint { get; set; }
        public List<int> CollectionIds { get; set; }
    }
}