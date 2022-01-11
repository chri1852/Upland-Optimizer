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
        Task<List<HistoryAction>> GetPropertyActionsFromTime(DateTime timeFrom, int minutesToAdd);
        Task<List<HistoryAction>> GetSendActionsFromTime(DateTime timeFrom, int minutesToAdd);
        Task<GetTransactionEntry> GetSingleTransactionById(string transactionId);
    }
}