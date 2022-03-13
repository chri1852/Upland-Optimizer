using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Upland.Interfaces.Processors
{
    public interface IBlockchainPropertySurfer
    {
        Task RunBlockChainUpdate();
        Task BuildBlockChainFromBegining();
        Task BuildBlockChainFromDate(long lastActionProcessed);
    }
}