using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Upland.Types.BlockchainTypes;

namespace Upland.Interfaces.Repositories
{
    public interface IBlockChainRepository
    {
        Task<List<dGood>> GetAllNFTs();
        Task<List<a24Entry>> GetSparkStakingTable();
        Task<List<a21Entry>> GetNftsRelatedToPropertys();
        Task<List<t2Entry>> GetForSaleProps();
        Task<List<t3Entry>> GetActiveOffers();
        Task<GetTransactionEntry> GetSingleTransactionById(string transactionId);
        Task<HistoryV2Query> GetPropertyActionsFromTime(DateTime fromTime, int minutesToAdd);
        Task<HistoryV2Query> GetSendActionsFromTime(DateTime fromTime, int minutesToAdd);
    }
}