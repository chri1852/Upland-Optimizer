using System;
using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class PlayUplandMeTransactionEntry
    {
        public DateTime block_time { get; set; }
        public List<PlayUplandMeTransactionEntrySubClass> traces { get; set; }
    }

    public class PlayUplandMeTransactionEntrySubClass
    {
        public PlayUplandMeActionEntry act { get; set; }
    }
}
