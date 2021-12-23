namespace Upland.Types.Types
{
    public class PropertySearchEntry
    {
        public long Id { get; set; }
        public int CityId { get; set; }
        public string Address { get; set; }
        public int StreetId { get; set; }
        public int? NeighborhoodId { get; set; }
        public int Size { get; set; }
        public double Mint { get; set; }
        public string? Status { get; set; }
        public bool FSA { get; set; }
        public string Owner { get; set; }
        public string Building { get; set; }
    }
}
