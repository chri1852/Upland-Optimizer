using System;
using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class GetTransactionEntry
    {
        public DateTime block_time { get; set; }
        public List<TRANSACTIONENTRY_SUBCLASS> traces { get; set; }
    }

    public class TRANSACTIONENTRY_SUBCLASS
    {
        public PlayUplandMeActionEntry act { get; set; }
    }
}
