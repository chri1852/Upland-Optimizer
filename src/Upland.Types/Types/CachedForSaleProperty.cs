using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class CachedForSaleProperty
    {
        public long Id { get; set; }
        public string Address { get; set; }
        public int CityId { get; set; }
        public int NeighborhoodId { get; set; }
        public int StreetId { get; set; }
        public int Size { get; set; }
        public bool FSA { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public string Owner { get; set; }
        public double Mint { get; set; }
        public double Markup { get; set; }
        public string Building { get; set; }
        public List<int> CollectionIds { get; set; }

        public double SortValue
        {
            get
            {
                return this.Currency == "USD" ? this.Price * 1000 : this.Price;
            }
        }
    }
}
