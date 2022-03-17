using System.Threading.Tasks;

namespace Upland.Interfaces.BlockchainSurfers
{
    public interface IUplandNFTActSurfer
    {
        Task RunBlockChainUpdate();
        Task BuildBlockChainFromBegining();
        Task ProcessBlockchainFromAction(long lastActionProcessed);
    }
}