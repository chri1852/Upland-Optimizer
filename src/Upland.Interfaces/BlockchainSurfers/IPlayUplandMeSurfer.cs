using System.Threading.Tasks;

namespace Upland.Interfaces.Processors
{
    public interface IPlayUplandMeSurfer
    {
        Task RunBlockChainUpdate();
        Task BuildBlockChainFromBegining();
        Task ProcessBlockchainFromAction(long lastActionProcessed);
    }
}