using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.BlockchainTypes
{
    public class GetEOSFlareActionsRequest
    {
        public string account_name { get; set; }
        public long pos { get; set; }
        public int offset { get; set; }
    }

    public class GetEOSFlareActionsResponse
    {
        public List<EOSFlareAction> actions { get; set; }
    }

    public class EOSFlareAction
    {
        public long account_action_seq { get; set; }
        public DateTime block_time { get; set; }
        public ActionTrace action_trace { get; set; }
    }

    public class ActionTrace
    {
        public ActionEntry act { get; set; }
        public string trx_id { get; set; }
    }
}
