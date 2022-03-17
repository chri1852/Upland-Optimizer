using System;

namespace Upland.Types.Types
{
    public class NFT
    {
        public int DGoodId { get; set; }
        public int NFTMetadataId { get; set; }
        public int SerialNumber { get; set; }
        public bool Burned { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? BurnedOn { get; set; }
        public byte[] Metadata { get; set; }
    }
}
