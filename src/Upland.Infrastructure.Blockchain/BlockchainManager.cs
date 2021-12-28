
using System;
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
            List<dGood> nfts = await blockchainRepository.GetAllNFTs();
            List<a21Entry> propStructs = await blockchainRepository.GetNftsRelatedToPropertys();
            List<PropertyStructure> returnStructures = new List<PropertyStructure>();

            nfts = nfts.Where(n => n.category == "structure").ToList();

            foreach (dGood nft in nfts)
            {
                returnStructures.Add(new PropertyStructure
                {
                    PropertyId = propStructs.Where(p => p.f45 == nft.id).First().a34,
                    StructureType = nft.token_name
                });
            }

            return returnStructures;
        }

        public async Task<List<long>> GetPropertiesUnderConstruction()
        {
            List<a21Entry> nftProp = await blockchainRepository.GetNftsRelatedToPropertys();
            List<a24Entry> stakes = await blockchainRepository.GetSparkStakingTable();
            List<long> underConstructionList = new List<long>();

            List<int> uniqueDGoodIds = stakes.GroupBy(s => s.f45).Select(g => g.First().f45).ToList();

            return nftProp.Where(p => uniqueDGoodIds.Contains(p.f45)).GroupBy(p => p.a34).Select(g => g.First().a34).ToList();
        }

        public async Task<Dictionary<string, double>> GetStakedSpark()
        {
            List<a24Entry> stakes = await blockchainRepository.GetSparkStakingTable();

            Dictionary<string, double> userStakes = new Dictionary<string, double>();

            foreach (a24Entry entry in stakes)
            {
                if (!userStakes.ContainsKey(entry.f34))
                {
                    userStakes.Add(entry.f34, double.Parse(entry.b14.Split(" ")[0]));
                }
                else
                {
                    userStakes[entry.f34] += double.Parse(entry.b14.Split(" ")[0]);
                }
            }

            return userStakes;
        }

        public async Task<List<HistoryAction>> GetPropertyActionsFromTime(DateTime timeFrom, int minutesToAdd)
        {
            return (await blockchainRepository.GetPropertyActionsFromTime(timeFrom, minutesToAdd)).actions;
        }

        public async Task<List<HistoryAction>> GetSendActionsFromTime(DateTime timeFrom, int minutesToAdd)
        {
            return (await blockchainRepository.GetSendActionsFromTime(timeFrom, minutesToAdd)).actions;
        }

        public async Task<GetTransactionEntry> GetSingleTransactionById(string transactionId)
        {
            return await blockchainRepository.GetSingleTransactionById(transactionId);
        }
    }
}
