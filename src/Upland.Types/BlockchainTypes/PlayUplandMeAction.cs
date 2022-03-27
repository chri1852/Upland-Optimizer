using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.BlockchainTypes
{
    public class GetPlayUplandMeActionsResponse
    {
        public List<PlayUplandMeAction> actions { get; set; }
    }

    public class PlayUplandMeAction
    {
        public long account_action_seq { get; set; }
        public DateTime block_time { get; set; }
        public bool irreversible { get; set; }
        public PlayUplandMeActionTrace action_trace { get; set; }
    }

    public class PlayUplandMeActionTrace
    {
        public PlayUplandMeActionEntry act { get; set; }
        public string trx_id { get; set; }
    }
}
