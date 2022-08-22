using System;
using System.Text.Json;
using Upland.Types.BlockchainTypes;

namespace BlockchainStoreApi.Types
{
    public class UspkTokenAccAction
    {
        public long Id { get; set; }
        public long AccountSequenceNumber { get; set; }
        public DateTime BlockTime { get; set; }
        public string TransactionId { get; set; }
        public string ActionName { get; set; }
        public string Data { get; set; }

        public long account_action_seq
        {
            get
            {
                return this.AccountSequenceNumber;
            }
        }

        public DateTime block_time
        {
            get
            {
                return this.BlockTime;
            }
        }

        public UspkTokenAccActionTrace action_trace
        {
            get
            {
                return new UspkTokenAccActionTrace
                {
                    act = new UspkTokenAccActionEntry
                    {
                        name = this.ActionName,
                        data = JsonSerializer.Deserialize<UspkTokenAccData>(this.Data)
                    },
                    trx_id = this.TransactionId
                };
            }
        }
    }
}