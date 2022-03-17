using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Upland.Interfaces.BlockchainSurfers;
using Upland.Interfaces.Managers;
using Upland.Types;
using Upland.Types.BlockchainTypes;
using Upland.Types.Types;

namespace Upland.BlockchainSurfer
{
    public class UplandNFTActSurfer : IUplandNFTActSurfer
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IBlockchainManager _blockchainManager;
        private readonly IUplandApiManager _uplandApiManager;
        private readonly string _uplandnftact;
        private readonly string _playuplandme;

        private List<SeriesTableEntry> _loadedSeries;
        private bool _isProcessing;

        public UplandNFTActSurfer(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager, IBlockchainManager blockchainManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;
            _blockchainManager = blockchainManager;
            _uplandnftact = "uplandnftact";
            _playuplandme = "playuplandme";

            _isProcessing = false;
            _loadedSeries = new List<SeriesTableEntry>();
        }

        public async Task RunBlockChainUpdate()
        {
            long lastActionProcessed = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUPLANDNFTACTACTIONSEQNUM));

            try
            {
                await ProcessBlockchainFromAction(lastActionProcessed);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - RunBlockChainUpdate", ex.Message);
                _isProcessing = false;
            }
        }

        public async Task BuildBlockChainFromBegining()
        {
            await ProcessBlockchainFromAction(-1);
        }

        public async Task ProcessBlockchainFromAction(long lastActionProcessed)
        {
            bool enableUpdates = bool.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUPLANDNFTACTACTIONSEQNUM));

            if (!enableUpdates || _isProcessing)
            {
                return;
            }

            _isProcessing = true;

            bool continueLoad = true;

            while (continueLoad)
            {
                List<UplandNFTActAction> actions = new List<UplandNFTActAction>();

                // Have to do this in a retry loop due to timeouts
                bool retry = true;
                while (retry)
                {
                    try
                    {
                        Thread.Sleep(2000);
                        actions = (await _blockchainManager.GetEOSFlareActions<GetUplandNFTActActionsResponse>(lastActionProcessed + 1, _uplandnftact)).actions;
                        if (actions != null)
                        {
                            retry = false;
                        }
                        else
                        {
                            Thread.Sleep(5000);
                        }
                    }
                    catch (Exception ex)
                    {
                        _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - BuildBlockChainFromDate - Loop", ex.Message);
                        Thread.Sleep(5000);
                    }
                }

                if (actions.Count == 0)
                {
                    // DEBUG
                    //continueLoad = false;
                }
                else
                {
                    try
                    {
                        // DEBUG
                        if (actions.Last().block_time > new DateTime(2021, 6, 20))
                        {
                            continueLoad = false;
                            break;
                        }
                        await ProcessActions(actions);
                    }
                    catch (Exception ex)
                    {
                        _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessActions - Exception Bubbled Up Disable Blockchain Updates", ex.Message);
                        _localDataManager.UpsertConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES, false.ToString());
                    }

                    lastActionProcessed = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUPLANDNFTACTACTIONSEQNUM));
                }
            }

            _isProcessing = false;
        }

        private async Task ProcessActions(List<UplandNFTActAction> actions)
        {
            long maxActionSeqNum = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUPLANDNFTACTACTIONSEQNUM));
            foreach (UplandNFTActAction action in actions)
            {
                if (action.account_action_seq < maxActionSeqNum)
                {
                    // We've already processed this event
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessActions", string.Format("Skipping Action {0} < {1}", action.account_action_seq, maxActionSeqNum));
                    continue;
                }

                // DEBUG
                if (action.block_time > new DateTime(2021, 6, 20))
                {
                    continue;
                }

                switch (action.action_trace.act.name)
                {
                    case "burnnft":
                        await ProcessBurnNFTAction(action);
                        break;
                    case "create":
                        await ProcessCreateAction(action);
                        break;
                    case "issue":
                        await ProcessIssueAction(action);
                        break;
                    case "transfernft":
                        await ProcessTransferNFTAction(action);
                        break;
                }

                if (action.account_action_seq > maxActionSeqNum)
                {
                    maxActionSeqNum = action.account_action_seq;
                    _localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXUPLANDNFTACTACTIONSEQNUM, action.account_action_seq.ToString());
                }
            }
        }

        private async Task ProcessBurnNFTAction(UplandNFTActAction action)
        {
            if (action.action_trace.act.data.dGood_Ids.Count > 1)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessBurnNFTAction", string.Format("Multiple DGoodIds Burned, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                return;
            }

            int dGoodId = action.action_trace.act.data.dGood_Ids.First();

            NFT burningNft = _localDataManager.GetNftByDGoodId(dGoodId);

            if (burningNft == null)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessBurnNFTAction", string.Format("No NFT to Burn, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                return;
            }

            burningNft.Burned = true;
            burningNft.BurnedOn = action.block_time;

            List<NFTHistory> nftHistory = _localDataManager.GetNftHistoryByDGoodId(dGoodId).Where(h => h.DisposedOn == null).ToList();

            foreach(NFTHistory history in nftHistory)
            {
                history.DisposedOn = action.block_time;
                _localDataManager.UpsertNftHistory(history);
            }
        }

        private async Task ProcessCreateAction(UplandNFTActAction action)
        {
            NFTMetadata newMetadata = new NFTMetadata
            {
                Id = -1,
                Name = action.action_trace.act.data.token_name,
                Category = action.action_trace.act.data.category,
                FullyLoaded = false
            };

            NFTMetadata existingMetadata = _localDataManager.GetNftMetadataByNameAndCategory(newMetadata.Name, newMetadata.Category);

            if (existingMetadata != null)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessCreateAction", string.Format("Existing NFTMetadataFound, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                return;
            }

            switch (newMetadata.Category)
            {
                case "blkexplorer":
                    newMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new BlockExplorerMetadata
                    {
                        DisplayName = action.action_trace.act.data.display_name,
                        Image = "",
                        SeriesId = action.action_trace.act.data.series_id,
                        SeriesName = await GetSeriesNameById(action.action_trace.act.data.series_id),
                        Description = "",
                        RarityLevel = "",
                        MaxSupply = int.Parse(action.action_trace.act.data.max_supply.Split(" UNFT")[0]),
                        MaxIssueDays = action.action_trace.act.data.max_issue_days
                    });
                    break;
                case "essential":
                    newMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new EssentialMetadata
                    {
                        DisplayName = action.action_trace.act.data.display_name,
                        Image = "",
                        TeamName = "",
                        FanPoints = 0,
                        Season = 0,
                        ModelType = "",
                        PlayerFullName = "",
                        PlayerPosition = "",
                        MaxSupply = int.Parse(action.action_trace.act.data.max_supply.Split(" UNFT")[0]),
                        MaxIssueDays = action.action_trace.act.data.max_issue_days
                    });
                    break;
                case "memento":
                    newMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new MementoMetadata
                    {
                        DisplayName = action.action_trace.act.data.display_name,
                        Image = "",
                        TeamName = "",
                        FanPoints = 0,
                        Season = 0,
                        ModelType = "",
                        PlayerFullName = "",
                        PlayerPosition = "",
                        MaxSupply = int.Parse(action.action_trace.act.data.max_supply.Split(" UNFT")[0]),
                        MaxIssueDays = action.action_trace.act.data.max_issue_days
                    });
                    break;
                case "spirithlwn":
                    newMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new SpirithlwnMetadata
                    {
                        DisplayName = action.action_trace.act.data.display_name,
                        Image = "",
                        RarityLevel = "",
                        MaxSupply = int.Parse(action.action_trace.act.data.max_supply.Split(" UNFT")[0]),
                        MaxIssueDays = action.action_trace.act.data.max_issue_days
                    });
                    break;
                case "structornmt":
                    newMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new StructornmtMetadata
                    {
                        DisplayName = action.action_trace.act.data.display_name,
                        Image = "",
                        RarityLevel = "",
                        BuildingType = "",
                        MaxSupply = int.Parse(action.action_trace.act.data.max_supply.Split(" UNFT")[0]),
                        MaxIssueDays = action.action_trace.act.data.max_issue_days
                    });
                    break;
                case "structure":
                    newMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new StructureMetadata
                    {
                        DisplayName = action.action_trace.act.data.display_name,
                        Image = "",
                        SparkHours = 0,
                        MinimumSpark = 0,
                        MaximumSpark = 0,
                    });
                    break;
                default:
                    throw new Exception("Unknown NFT Category Detected Stoping Updates");
            }

            _localDataManager.UpsertNftMetadata(newMetadata);
        }

        private async Task ProcessIssueAction(UplandNFTActAction action)
        {
            if (action.action_trace.act.data.dGood_Ids.Count > 1)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessIssueAction", string.Format("Multiple DGoodIds Issued, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                return;
            }

            int dGoodId = action.action_trace.act.data.dGood_Ids.First();

            NFT existingNft = _localDataManager.GetNftByDGoodId(dGoodId);

            if (existingNft != null)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessIssueAction", string.Format("NFT Already Exists, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                return;
            }

            NFTMetadata nftMetadata = _localDataManager.GetNftMetadataByNameAndCategory(action.action_trace.act.data.token_name, action.action_trace.act.data.category);

            if (nftMetadata != null)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessIssueAction", string.Format("No NFT Metadata Found, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                return;
            }

            NFT newNft = new NFT
            {
                DGoodId = dGoodId,
                NFTMetadataId = nftMetadata.Id,
                SerialNumber = 0,
                Burned = false,
                CreatedOn = action.block_time,
                BurnedOn = null,
                FullyLoaded = false,
            };

            switch (nftMetadata.Category)
            {
                case "blkexplorer":
                    break;
                case "essential":
                    break;
                case "memento":
                    break;
                case "spirithlwn":
                    break;
                case "structornmt":
                    break;
                case "structure":
                    break;
                default:
                    throw new Exception("Unknown NFT Category Detected Stoping Updates");
            }
        }

        private async Task ProcessTransferNFTAction(UplandNFTActAction action)
        {

        }

        private async Task<string> GetSeriesNameById(int id)
        {
            if (_loadedSeries == null || _loadedSeries.Count == 0 || !_loadedSeries.Any(s => s.id == id))
            {
                _loadedSeries = await _blockchainManager.GetSeriesTable();
            }

            if (_loadedSeries == null || _loadedSeries.Count == 0)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessActions", string.Format("Could Not Load Series Table, Id: {0}", id));
                return "Unknown";
            }

            SeriesTableEntry matchingEntry = _loadedSeries.FirstOrDefault(s => s.id == id);

            if (matchingEntry == null)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessActions", string.Format("Could Not Find Series Id, Id: {0}", id));
                return "Unknown";
            }

            return matchingEntry.name;
        }
    }
}