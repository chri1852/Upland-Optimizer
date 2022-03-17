using System;
using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class GetUplandNFTActActionsResponse
    {
        public List<UplandNFTActAction> actions { get; set; }
    }

    public class UplandNFTActAction
    {
        public long account_action_seq { get; set; }
        public DateTime block_time { get; set; }
        public UplandNFTActActionTrace action_trace { get; set; }
    }

    public class UplandNFTActActionTrace
    {
        public UplandNFTActActionEntry act { get; set; }
        public string trx_id { get; set; }
    }
}