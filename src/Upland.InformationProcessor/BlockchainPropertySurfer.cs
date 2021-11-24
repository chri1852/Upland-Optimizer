using System;
using System.Collections.Generic;
using System.Text;
using Upland.Infrastructure.Blockchain;
using Upland.Infrastructure.LocalData;

namespace Upland.InformationProcessor
{
    public class BlockchainPropertySurfer
    {
        private readonly LocalDataManager localDataManager;
        private readonly BlockchainManager blockchainManager;

        public BlockchainPropertySurfer()
        {
            localDataManager = new LocalDataManager();
            blockchainManager = new BlockchainManager();
        }
    }
}
