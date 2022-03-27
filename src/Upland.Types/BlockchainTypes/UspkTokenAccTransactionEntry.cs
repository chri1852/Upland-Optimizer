using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.BlockchainTypes
{
    public class UspkTokenAccTransactionEntry
    {
        public DateTime block_time { get; set; }
        public List<UspkTokenAccTransactionEntrySubClass> traces { get; set; }
    }

    public class UspkTokenAccTransactionEntrySubClass
    {
        public UspkTokenAccActionEntry act { get; set; }
    }
}
