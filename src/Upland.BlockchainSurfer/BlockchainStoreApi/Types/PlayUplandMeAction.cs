using System;
using System.Text.Json;
using Upland.Types.BlockchainTypes;

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

        public PlayUplandMeActionTrace action_trace
        {
            get
            {
                return new PlayUplandMeActionTrace
                {
                    act = new PlayUplandMeActionEntry
                    {
                        name = this.ActionName,
                        data = JsonSerializer.Deserialize<PlayUplandMeData>(this.Data)
                    },
                    trx_id = this.TransactionId
                };
            }
        }

    }

    public class PlayUplandMeActionTrace
    {
        public PlayUplandMeActionEntry act { get; set; }
        public string trx_id { get; set; }
    }

    public class PlayUplandMeActionEntry
    {
        public string name { get; set; }
        public PlayUplandMeData data { get; set; }
    }
}