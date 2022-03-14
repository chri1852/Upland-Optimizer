using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.BlockchainTypes
{
    public class GetUspkTokenAccActionsResponse
    {
        public List<UspkTokenAccAction> actions { get; set; }
    }

    public class UspkTokenAccAction
    {
        public long account_action_seq { get; set; }
        public DateTime block_time { get; set; }
        public PlayUplandMeActionTrace action_trace { get; set; }
    }

    public class UspkTokenAccActionTrace
    {
        public UspkTokenAccActionEntry act { get; set; }
        public string trx_id { get; set; }
    }
}