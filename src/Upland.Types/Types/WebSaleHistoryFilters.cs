using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class WebSaleHistoryFilters
    {
        public string SearchByType { get; set; }
        public string SearchByUsername { get; set; }
        public int SearchByCityId { get; set; }

        public List<int> NeighborhoodIds { get; set; }
        public List<int> CollectionIds { get; set; }
        public string Address { get; set; }

        public bool Asc { get; set; }
        public string OrderBy { get; set; }

        public List<string> EntryType { get; set; }
        public string Currency { get; set; }

        public int PageSize { get; set; }
        public int Page { get; set; }
    }
}
