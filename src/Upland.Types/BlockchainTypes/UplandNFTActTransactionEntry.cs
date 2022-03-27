using System;
using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class UplandNFTActTransactionEntry
    {
        public DateTime block_time { get; set; }
        public List<UplandNFTActTransactionEntrySubClass> traces { get; set; }
    }

    public class UplandNFTActTransactionEntrySubClass
    {
        public UplandNFTActActionEntry act { get; set; }
    }
}
