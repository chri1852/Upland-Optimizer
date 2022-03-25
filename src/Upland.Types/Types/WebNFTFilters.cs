namespace Upland.Types.Types
{
    public class WebNFTFilters
    {
        public WebNFT Filters { get; set; }
        public bool IncludeBurned { get; set; }
        public string SortBy { get; set; }
        public bool SortDescending { get; set; }
        public string Category { get; set; }
        public int PageSize { get; set; }
        public int Page { get; set; }
        public bool NoPaging { get; set; }
    }
}
