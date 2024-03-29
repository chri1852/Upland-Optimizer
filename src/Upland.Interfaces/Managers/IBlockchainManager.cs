﻿using System;
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
        Task<List<dGood>> GetAllDGoodsFromTable();
        Task<dGood> GetDGoodFromTable(int dGoodId);
        Task<List<SeriesTableEntry>> GetSeriesTable();
        Task<List<a15Entry>> GetPropertyTable();
        Task<T> GetSingleTransactionById<T>(string transactionId);
        Task<T> GetEOSFlareActions<T>(long position, string accountName);
    }
}