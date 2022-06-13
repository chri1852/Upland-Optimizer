using System.Collections.Generic;
using System.Text.Json;

namespace Upland.Types.Types
{
    public class City
    {
        public int CityId { get; set; }
        public string Name { get; set; }
        public string? SquareCoordinates { get; set; }
        public string StateCode { get; set; }
        public string CountryCode { get; set; }

        // N, S, E, W
        public List<double> GetCityCoordinates()
        {
            if (this.SquareCoordinates == null)
            {
                return new List<double> { 0, 0, 0, 0 };
            }
            
            return JsonSerializer.Deserialize<List<double>>(this.SquareCoordinates);
        }
    }
}
