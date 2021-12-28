using System;
using System.Collections.Generic;

namespace Upland.Types.BlockchainTypes
{
    public class HistoryV2Query
    {
        public List<HistoryAction> actions { get; set; }
    }

    public class HistoryAction
    {
        public DateTime timestamp { get; set; }
        public ActionEntry act { get; set; }
        public long global_sequence { get; set; }
        public string trx_id { get; set; }
    }

    public class GetTransactionEntry
    {
        public List<TRANSACTIONENTRY_SUBCLASS> traces { get; set; }
    }

    public class TRANSACTIONENTRY_SUBCLASS
    {
        public ActionEntry act { get; set; }
    }

    public class ActionEntry
    {
        public string name { get; set; }
        public ActionData data { get; set; }
    }

    public class ActionData
    {
        public string memo { get; set; }
        public string a45 { get; set; }
        public string a54 { get; set; }
        public string p3 { get; set; }
        public string p11 { get; set; }
        public string p14 { get; set; }
        public string p15 { get; set; }
        public List<string> p21 { get; set; }
        public string p23 { get; set; }
        public string p24 { get; set; }
        public string p25 { get; set; }
        public string p44 { get; set; }
        public string p51 { get; set; }
        public string p52 { get; set; }
        public string p53 { get; set; }
        public string p54 { get; set; }
    }
}
