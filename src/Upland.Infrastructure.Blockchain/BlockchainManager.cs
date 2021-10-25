
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Upland.Types.BlockchainTypes;
using Upland.Types.Types;

namespace Upland.Infrastructure.Blockchain
{
    public class BlockchainManager
    {
        private BlockchainRepository blockchainRepository;

        public BlockchainManager()
        {
            blockchainRepository = new BlockchainRepository();
        }

        public async Task<List<PropertyStructure>> GetPropertyStructures()
        {
            List<dGood> nfts =  await blockchainRepository.GetAllNFTs();
            List<a21Entry> propStructs = await blockchainRepository.GetNftsRelatedToPropertys();
            List<PropertyStructure> returnStructures = new List<PropertyStructure>();

            nfts = nfts.Where(n => n.category == "structure").ToList();

            foreach(dGood nft in nfts)
            {
                returnStructures.Add(new PropertyStructure
                {
                    PropertyId = propStructs.Where(p => p.f45 == nft.id).First().a34,
                    StructureType = nft.token_name
                });
            }

            return returnStructures;
        }
    }
}
