using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Upland.Interfaces.BlockchainSurfers
{
    public interface IUSPKTokenAccSurfer
    {
        Task RunBlockChainUpdate();
        Task BuildBlockChainFromBegining();
        Task ProcessBlockchainFromAction(long lastActionProcessed);
    }
}