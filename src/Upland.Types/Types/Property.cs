using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types
{
    public class Property
    {
        public long Id { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public int StreetId { get; set; }
        public int Size { get; set; }
        public decimal MonthlyEarnings { get; set; }
    }
}
