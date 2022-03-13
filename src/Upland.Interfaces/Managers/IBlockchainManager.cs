using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.Types.BlockchainTypes;
using Upland.Types.Types;

namespace Upland.Interfaces.Managers
{
    public interface IBlockchainManager
    {
        Task<List<PropertyStructure>> GetPropertyStructures();
        Task<List<long>> GetPropertiesUnderConstruction();
        Task<Dictionary<string, double>> GetStakedSpark();
        Task<GetTransactionEntry> GetSingleTransactionById(string transactionId);
        Task<List<EOSFlareAction>> GetEOSFlareActions(long position, string accountName);
    }
}