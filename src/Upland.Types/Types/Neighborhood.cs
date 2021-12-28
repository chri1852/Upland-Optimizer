using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class Neighborhood
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CityId { get; set; }
        public List<List<List<List<double>>>> Coordinates { get; set; }

        // For loading from upland
        public int City_Id { get; set; }
        public NeighborhoodCoordinates Boundaries { get; set; }
    }

    public class NeighborhoodCoordinates
    {
        public string Type { get; set; }
        public object Coordinates { get; set; }
    }
}
