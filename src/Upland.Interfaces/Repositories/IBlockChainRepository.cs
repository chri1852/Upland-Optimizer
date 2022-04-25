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
        Task<dGood> GetDGoodFromTable(int dGoodId);
        Task<List<a24Entry>> GetSparkStakingTable();
        Task<List<a21Entry>> GetNftsRelatedToPropertys();
        Task<List<t2Entry>> GetForSaleProps();
        Task<List<a15Entry>> GetPropertyTable();
        Task<List<t3Entry>> GetActiveOffers();
        Task<List<SeriesTableEntry>> GetSeriesTable();
        Task<T> GetSingleTransactionById<T>(string transactionId);
        Task<T> GetEOSFlareActions<T>(long position, string accountName);
    }
}