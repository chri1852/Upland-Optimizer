using System.Collections.Generic;

namespace BlockchainStoreApi.Messages
{
    internal class GetActionsResponse<T>
    {
        public List<T> actions { get; set; }
        public bool BlockchainUpdatesEnabled { get; set; }
    }
}
