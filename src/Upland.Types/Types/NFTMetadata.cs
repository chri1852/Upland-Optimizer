namespace Upland.Types.Types
{
    public class NFTMetadata
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public bool FullyLoaded { get; set; }
        public byte[] Metadata { get; set; }
    }
}
