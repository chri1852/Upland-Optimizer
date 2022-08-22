using System;

namespace BlockchainStoreApi.Types
{
    public class PlayUplandMeAction
    {
        public long Id { get; set; }
        public long AccountSequenceNumber { get; set; }
        public DateTime BlockTime { get; set; }
        public string TransactionId { get; set; }
        public string ActionName { get; set; }
        public string Data { get; set; }
    }
}