using System;
using System.Text.Json;
using Upland.Types.BlockchainTypes;

namespace BlockchainStoreApi.Types
{
    public class UplandNFTActAction
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

        public UplandNFTActActionTrace action_trace
        {
            get
            {
                return new UplandNFTActActionTrace
                {
                    act = new UplandNFTActActionEntry
                    {
                        name = this.ActionName,
                        data = JsonSerializer.Deserialize<UplandNFTActData>(this.Data)
                    },
                    trx_id = this.TransactionId
                };
            }
        }
    }
}