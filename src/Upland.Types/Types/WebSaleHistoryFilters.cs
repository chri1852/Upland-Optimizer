using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class WebSaleHistoryFilters
    {
        public bool CityIdSearch { get; set; }
        public string SearchByUsername { get; set; }
        public int SearchByCityId { get; set; }

        public bool NoSales { get; set; }
        public bool NoSwaps { get; set; }
        public bool NoOffers { get; set; }

        public string Currency { get; set; }
        public string Address { get; set; }
        public string Username { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public List<int> NeighborhoodIds { get; set; }
        public List<int> CollectionIds { get; set; }

        public int PageSize { get; set; }
        public int Page { get; set; }
    }
}
