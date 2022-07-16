using System;
using System.Collections.Generic;
using System.Linq;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Enums;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class LeaderboardProcessor : ILeaderboardProcessor
    {
        private const int _cacheTimeInMinutes = 15;
        private readonly ILocalDataManager _localDataManager;
        private readonly ICachingProcessor _cachingProcessor;

        private Dictionary<LeaderboardTypeEnum, Tuple<DateTime, List<LeaderboardListItem>>> _leaderboardCache;

        public LeaderboardProcessor(ILocalDataManager localDataManager, ICachingProcessor cachingProcessor)
        {
            _localDataManager = localDataManager;
            _cachingProcessor = cachingProcessor;

            InitializeCache();
        }

        private void InitializeCache()
        {
            _leaderboardCache = new Dictionary<LeaderboardTypeEnum, Tuple<DateTime, List<LeaderboardListItem>>>();

            _leaderboardCache.Add(LeaderboardTypeEnum.Spark, new Tuple<DateTime, List<LeaderboardListItem>>(DateTime.UtcNow.AddDays(-1), new List<LeaderboardListItem>()));
            _leaderboardCache.Add(LeaderboardTypeEnum.PropCount, new Tuple<DateTime, List<LeaderboardListItem>>(DateTime.UtcNow.AddDays(-1), new List<LeaderboardListItem>()));
            _leaderboardCache.Add(LeaderboardTypeEnum.MonthlyEarnings, new Tuple<DateTime, List<LeaderboardListItem>>(DateTime.UtcNow.AddDays(-1), new List<LeaderboardListItem>()));
            _leaderboardCache.Add(LeaderboardTypeEnum.Size, new Tuple<DateTime, List<LeaderboardListItem>>(DateTime.UtcNow.AddDays(-1), new List<LeaderboardListItem>()));
            _leaderboardCache.Add(LeaderboardTypeEnum.Mint, new Tuple<DateTime, List<LeaderboardListItem>>(DateTime.UtcNow.AddDays(-1), new List<LeaderboardListItem>()));
            _leaderboardCache.Add(LeaderboardTypeEnum.CollectionProps, new Tuple<DateTime, List<LeaderboardListItem>>(DateTime.UtcNow.AddDays(-1), new List<LeaderboardListItem>()));
        }

        public List<LeaderboardListItem> GetLeaderboardByType(LeaderboardTypeEnum type, DateTime fromTime, string additionalInfo = null)
        {
            switch(type)
            {
                case LeaderboardTypeEnum.USDSale:
                case LeaderboardTypeEnum.UPXSale:
                case LeaderboardTypeEnum.PropsMinted:
                case LeaderboardTypeEnum.MintedUpx:
                case LeaderboardTypeEnum.SpentUPX:
                case LeaderboardTypeEnum.SpentUSD:
                case LeaderboardTypeEnum.NetUSD:
                    return _localDataManager.GetLeaderboardByType(type, fromTime);

                case LeaderboardTypeEnum.Spark:
                case LeaderboardTypeEnum.PropCount:
                case LeaderboardTypeEnum.MonthlyEarnings:
                case LeaderboardTypeEnum.Size:
                case LeaderboardTypeEnum.Mint:
                case LeaderboardTypeEnum.CollectionProps:
                    return GetLeaderboardListFromCache(type);

                case LeaderboardTypeEnum.NFLPALegitFanPoints:
                    return GetNFLPALegitsFanPointsLeaders(additionalInfo);

                case LeaderboardTypeEnum.NFTCount:
                    return GetNftCountLeaders(additionalInfo);

                default:
                    return new List<LeaderboardListItem>();
            }
        }

        private List<LeaderboardListItem> GetLeaderboardListFromCache(LeaderboardTypeEnum type)
        {
            if (_leaderboardCache[type].Item1 < DateTime.UtcNow)
            {
                _leaderboardCache[type] = new Tuple<DateTime, List<LeaderboardListItem>>(
                    DateTime.UtcNow.AddMinutes(_cacheTimeInMinutes),
                    _localDataManager.GetLeaderboardByType(type, DateTime.UtcNow));
            }

            return _leaderboardCache[type].Item2;
        }

        private List<LeaderboardListItem> GetNFLPALegitsFanPointsLeaders(string teamName)
        {
            Dictionary<int, EssentialMetadata> essentialMetadataDictionary = _cachingProcessor.GetEssentialMetadataFromCache();
            Dictionary<int, MementoMetadata> mementoMetadataDictionary = _cachingProcessor.GetMementoMetadataFromCache();

            List<NFT> matchingEssentialsNfts = _localDataManager.GetNFTsByNFTMetadataId(essentialMetadataDictionary
                .Select(m => m.Key).ToList()
            ).Where(n => !n.Burned && n.Owner != null && n.Owner != "").ToList();
            List<NFT> matchingMementosNfts = _localDataManager.GetNFTsByNFTMetadataId(mementoMetadataDictionary
                .Select(m => m.Key).ToList()
            ).Where(n => !n.Burned && n.Owner != null && n.Owner != "").ToList();

            Dictionary<Tuple<string, string>, double> fanPointsDictionary = new Dictionary<Tuple<string, string>, double>();

            foreach (NFT nft in matchingEssentialsNfts)
            {
                Tuple<string, string> key = new Tuple<string, string>(essentialMetadataDictionary[nft.NFTMetadataId].TeamName, nft.Owner);
                if (fanPointsDictionary.ContainsKey(key))
                {
                    fanPointsDictionary[key] += essentialMetadataDictionary[nft.NFTMetadataId].FanPoints;
                }
                else
                {
                    fanPointsDictionary.Add(key, essentialMetadataDictionary[nft.NFTMetadataId].FanPoints);
                }
            }

            foreach (NFT nft in matchingMementosNfts)
            {
                Tuple<string, string> key = new Tuple<string, string>(mementoMetadataDictionary[nft.NFTMetadataId].TeamName, nft.Owner);
                if (fanPointsDictionary.ContainsKey(key))
                {
                    fanPointsDictionary[key] += mementoMetadataDictionary[nft.NFTMetadataId].FanPoints;
                }
                else
                {
                    fanPointsDictionary.Add(key, mementoMetadataDictionary[nft.NFTMetadataId].FanPoints);
                }
            }

            List<LeaderboardListItem> leadboard = new List<LeaderboardListItem>();

            if (teamName != "All")
            {
                leadboard = fanPointsDictionary
                    .Where(f => f.Value != 0 && f.Key.Item2 != "" && f.Key.Item1 == teamName)
                    .Select(f => new LeaderboardListItem()
                    {
                        Rank = -1,
                        UplandUsername = f.Key.Item2,
                        Value = f.Value,
                        AdditionalInformation = f.Key.Item1
                    })
                    .OrderByDescending(f => f.Value)
                    .ToList();
            }
            else
            {
                leadboard = fanPointsDictionary
                    .Where(f => f.Value != 0 && f.Key.Item2 != "")
                    .Select(f => new LeaderboardListItem()
                    {
                        Rank = -1,
                        UplandUsername = f.Key.Item2,
                        Value = f.Value,
                        AdditionalInformation = f.Key.Item1
                    })
                    .OrderByDescending(f => f.Value)
                    .ToList();
            }

            return _localDataManager.CalculateLeaderboardListRanks(leadboard);
        }

        private List<LeaderboardListItem> GetNftCountLeaders(string nftType)
        {
            List<int> nftMetadataIds = new List<int>();
            if (nftType == "All")
            {
                nftMetadataIds.AddRange(_cachingProcessor.GetStructureMetadataFromCache().Select(m => m.Key).ToList());
                nftMetadataIds.AddRange(_cachingProcessor.GetStructornmtMetadataFromCache().Select(m => m.Key).ToList());
                nftMetadataIds.AddRange(_cachingProcessor.GetSpiritHlwnMetadataFromCache().Select(m => m.Key).ToList());
                nftMetadataIds.AddRange(_cachingProcessor.GetMementoMetadataFromCache().Select(m => m.Key).ToList());
                nftMetadataIds.AddRange(_cachingProcessor.GetEssentialMetadataFromCache().Select(m => m.Key).ToList());
                nftMetadataIds.AddRange(_cachingProcessor.GetBlockExplorerMetadataFromCache().Select(m => m.Key).ToList());
                nftMetadataIds.AddRange(_cachingProcessor.GetLandVehicleMetadataFromCache().Select(m => m.Key).ToList());
            }
            else
            {
                switch(nftType)
                {
                    case Consts.METADATA_TYPE_STRUCTURE:
                        nftMetadataIds = _cachingProcessor.GetStructureMetadataFromCache().Select(m => m.Key).ToList();
                        break;
                    case Consts.METADATA_TYPE_STRUCTORNMT:
                        nftMetadataIds = _cachingProcessor.GetStructornmtMetadataFromCache().Select(m => m.Key).ToList();
                        break;
                    case Consts.METADATA_TYPE_SPIRITHLWN:
                        nftMetadataIds = _cachingProcessor.GetSpiritHlwnMetadataFromCache().Select(m => m.Key).ToList();
                        break;
                    case Consts.METADATA_TYPE_MEMENTO:
                        nftMetadataIds = _cachingProcessor.GetMementoMetadataFromCache().Select(m => m.Key).ToList();
                        break;
                    case Consts.METADATA_TYPE_ESSENTIAL:
                        nftMetadataIds = _cachingProcessor.GetEssentialMetadataFromCache().Select(m => m.Key).ToList();
                        break;
                    case Consts.METADATA_TYPE_BLKEXPLORER:
                        nftMetadataIds = _cachingProcessor.GetBlockExplorerMetadataFromCache().Select(m => m.Key).ToList();
                        break;
                    case Consts.METADATA_TYPE_LANDVEHICLE:
                        nftMetadataIds = _cachingProcessor.GetLandVehicleMetadataFromCache().Select(m => m.Key).ToList();
                        break;
                }
            }

            List<NFT> matchingNFTs = _localDataManager.GetNFTsByNFTMetadataId(nftMetadataIds);
            Dictionary<string, int> nftCountDictionary = new Dictionary<string, int>();

            foreach (NFT nft in matchingNFTs)
            {
                if (nftCountDictionary.ContainsKey(nft.Owner))
                {
                    nftCountDictionary[nft.Owner] += 1;
                }
                else
                {
                    nftCountDictionary.Add(nft.Owner, 1);
                }
            }

            List<LeaderboardListItem> leadboard = new List<LeaderboardListItem>();

            leadboard = nftCountDictionary
                .Where(f => f.Value != 0 && f.Key != "")
                .Select(f => new LeaderboardListItem()
                {
                    Rank = -1,
                    UplandUsername = f.Key,
                    Value = f.Value,
                    AdditionalInformation = ""
                })
                .OrderByDescending(f => f.Value)
                .ToList();

            return _localDataManager.CalculateLeaderboardListRanks(leadboard);
        }
    }
}
