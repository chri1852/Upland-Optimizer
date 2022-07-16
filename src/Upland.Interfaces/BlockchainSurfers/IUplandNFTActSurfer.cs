using System.Threading.Tasks;
using Upland.Types.Types;

namespace Upland.Interfaces.BlockchainSurfers
{
    public interface IUplandNFTActSurfer
    {
        Task RunBlockChainUpdate();
        Task BuildBlockChainFromBegining();
        Task ProcessBlockchainFromAction(long lastActionProcessed);
        Task TryLoadNFTByDGoodId(int dGoodId);
    }
}