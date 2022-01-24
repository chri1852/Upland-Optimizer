using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class WebForSaleFilters
    {
        public int CityId { get; set; }
        public string Owner { get; set; }
        public string Address { get; set; }

        public List<int> NeighborhoodIds { get; set; }
        public List<int> CollectionIds { get; set; }
        public List<string> Buildings { get; set; }
        public bool? FSA { get; set; }

        public bool Asc { get; set; }
        public string OrderBy { get; set; }

        public int PageSize { get; set; }
        public int Page { get; set; }
    }
}
