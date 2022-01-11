using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.Types.BlockchainTypes;

namespace Upland.Interfaces.Processors
{
    public interface IBlockchainSendFinder
    {
        Task RunBlockChainUpdate();
        Task SearchSendsFromDate(DateTime startDate);
        void ProcessActions(List<HistoryAction> actions, List<Tuple<decimal, string, string>> registeredUserEOSAccounts);
    }
}