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
        private List<dGood> _currentdGoodTable;
        private DateTime _dGoodTableExpirationDate;

        public UplandNFTActSurfer(ILocalDataManager localDataManager, IUplandApiManager uplandApiManager, IBlockchainManager blockchainManager)
        {
            _localDataManager = localDataManager;
            _uplandApiManager = uplandApiManager;
            _blockchainManager = blockchainManager;
            _uplandnftact = "uplandnftact";
            _playuplandme = "playuplandme";

            _isProcessing = false;
            _loadedSeries = new List<SeriesTableEntry>();
            _currentdGoodTable = null;
            _dGoodTableExpirationDate = DateTime.UtcNow.AddDays(-1);
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
            bool enableUpdates = bool.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES));

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
                        Thread.Sleep(5000);
                    }
                }

                if (actions.Count < 10)
                {
                    continueLoad = false;
                }

                try
                {
                    await ProcessActions(actions);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessActions - Exception Bubbled Up Disable Blockchain Updates", ex.Message);
                    _localDataManager.UpsertConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES, false.ToString());
                }

                lastActionProcessed = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUPLANDNFTACTACTIONSEQNUM));    
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

                switch (action.action_trace.act.name)
                {
                    case "burnnft":
                        ProcessBurnNFTAction(action);
                        break;
                    case "create":
                        await ProcessCreateAction(action);
                        break;
                    case "issue":
                        await ProcessIssueAction(action);
                        break;
                    case "transfernft":
                        ProcessTransferNFTAction(action);
                        break;
                }

                if (action.account_action_seq > maxActionSeqNum)
                {
                    maxActionSeqNum = action.account_action_seq;
                    _localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXUPLANDNFTACTACTIONSEQNUM, action.account_action_seq.ToString());
                }
            }
        }

        private void ProcessBurnNFTAction(UplandNFTActAction action)
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
            _localDataManager.UpsertNft(burningNft);

            List<NFTHistory> nftHistory = _localDataManager.GetNftHistoryByDGoodId(dGoodId).Where(h => h.DisposedOn == null).ToList();

            foreach (NFTHistory history in nftHistory)
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
                        SeriesId = action.action_trace.act.data.series_id.HasValue ? action.action_trace.act.data.series_id.Value : 0,
                        SeriesName = action.action_trace.act.data.series_id.HasValue ? await GetSeriesNameById(action.action_trace.act.data.series_id.Value) : "",
                        Description = "",
                        RarityLevel = "",
                        MaxSupply = int.Parse(action.action_trace.act.data.max_supply.Split(" UNFT")[0]),
                        MaxIssueDays = action.action_trace.act.data.max_issue_days > int.MaxValue ? int.MaxValue : (int)action.action_trace.act.data.max_issue_days
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
                        MaxIssueDays = action.action_trace.act.data.max_issue_days > int.MaxValue ? int.MaxValue : (int)action.action_trace.act.data.max_issue_days
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
                        MaxIssueDays = action.action_trace.act.data.max_issue_days > int.MaxValue ? int.MaxValue : (int)action.action_trace.act.data.max_issue_days
                    });
                    break;
                case "spirithlwn":
                    newMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new SpirithlwnMetadata
                    {
                        DisplayName = action.action_trace.act.data.display_name,
                        Image = "",
                        RarityLevel = "",
                        MaxSupply = int.Parse(action.action_trace.act.data.max_supply.Split(" UNFT")[0]),
                        MaxIssueDays = action.action_trace.act.data.max_issue_days > int.MaxValue ? int.MaxValue : (int)action.action_trace.act.data.max_issue_days
                    });
                    break;
                case "structornmt":
                    newMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new StructornmtMetadata
                    {
                        DisplayName = action.action_trace.act.data.display_name,
                        Image = "",
                        RarityLevel = "",
                        BuildingType = "",
                        DecorationId = 0,
                        MaxSupply = int.Parse(action.action_trace.act.data.max_supply.Split(" UNFT")[0]),
                        MaxIssueDays = action.action_trace.act.data.max_issue_days > int.MaxValue ? int.MaxValue : (int)action.action_trace.act.data.max_issue_days
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

            NFTMetadata nftMetadata = _localDataManager.GetNftMetadataByNameAndCategory(action.action_trace.act.data.token_name, action.action_trace.act.data.category);

            if (nftMetadata == null)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessIssueAction", string.Format("No NFT Metadata Found, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                return;
            }

            if (existingNft != null && nftMetadata.Category != Consts.METADATA_TYPE_STRUCTURE)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessIssueAction", string.Format("NFT Already Exists, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
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

            if (nftMetadata.Category == Consts.METADATA_TYPE_STRUCTURE)
            {
                if (existingNft == null)
                {
                    // Need to wait for the transfer action to process these
                    newNft.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new StructureSpecificMetaData());
                    _localDataManager.UpsertNft(newNft);
                }
            }
            else
            {
                await PopulateNFTAndNFTMetadata(newNft, nftMetadata);
            }

            if (action.action_trace.act.data.to != _playuplandme)
            {
                NFTHistory newHistoryEntry = new NFTHistory
                {
                    Id = -1,
                    DGoodId = dGoodId,
                    Owner = action.action_trace.act.data.to,
                    ObtainedOn = action.block_time,
                    DisposedOn = null
                };
                _localDataManager.UpsertNftHistory(newHistoryEntry);
            }
        }

        private async Task ProcessTransferNFTAction(UplandNFTActAction action)
        {
            if (action.action_trace.act.data.dGood_Ids.Count > 1)
            {
                //_localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessTransferNFTAction", string.Format("Multiple Transfered, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
               // return;
            }

            foreach (int dGoodId in action.action_trace.act.data.dGood_Ids)
            {
                NFT transferingNft = _localDataManager.GetNftByDGoodId(dGoodId);

                if (transferingNft == null && Regex.Match(action.action_trace.act.data.memo, "^BUILD,").Success)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessTransferNFTAction", string.Format("No NFT to Transfer, ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                    continue;
                }

                if (Regex.Match(action.action_trace.act.data.memo, "^BUILD,").Success)
                {
                    string name = action.action_trace.act.data.memo.Split(",")[9];
                    string category = action.action_trace.act.data.memo.Split(",")[8];
                    NFTMetadata structureMetadata = _localDataManager.GetNftMetadataByNameAndCategory(name, category);

                    if (!structureMetadata.FullyLoaded)
                    {
                        StructureMetadata currentMetadata = HelperFunctions.HelperFunctions.DecodeMetadata<StructureMetadata>(structureMetadata.Metadata);
                        currentMetadata.Image = @"https://static.upland.me/" + currentMetadata.DisplayName.Replace(" ", "_").ToLower() + "/blank.png";
                        currentMetadata.SparkHours = int.Parse(action.action_trace.act.data.memo.Split(",")[5]);
                        currentMetadata.MinimumSpark = decimal.Parse(action.action_trace.act.data.memo.Split(",")[6]) / 100;
                        currentMetadata.MaximumSpark = decimal.Parse(action.action_trace.act.data.memo.Split(",")[7]) / 100;
                        structureMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(currentMetadata);
                        structureMetadata.FullyLoaded = true;

                        _localDataManager.UpsertNftMetadata(structureMetadata);
                    }

                    if (!transferingNft.FullyLoaded)
                    {
                        transferingNft.FullyLoaded = true;
                        StructureSpecificMetaData structMetadata = HelperFunctions.HelperFunctions.DecodeMetadata<StructureSpecificMetaData>(transferingNft.Metadata);
                        structMetadata.PropertyId = long.Parse(action.action_trace.act.data.memo.Split(string.Format("BUILD,{0},", dGoodId))[1].Split(",")[0]);
                        transferingNft.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(structMetadata);

                        _localDataManager.UpsertNft(transferingNft);
                    }
                }

                List<NFTHistory> nftHistory = _localDataManager.GetNftHistoryByDGoodId(dGoodId);

                if (action.action_trace.act.data.to == _playuplandme)
                {
                    foreach (NFTHistory history in nftHistory.Where(h => h.DisposedOn == null).ToList())
                    {
                        history.DisposedOn = action.block_time;
                        _localDataManager.UpsertNftHistory(history);
                    }
                }
                else if (action.action_trace.act.data.from == _playuplandme)
                {
                    NFTHistory newHistoryEntry = new NFTHistory
                    {
                        Id = -1,
                        DGoodId = dGoodId,
                        Owner = action.action_trace.act.data.to,
                        ObtainedOn = action.block_time,
                        DisposedOn = null
                    };
                    _localDataManager.UpsertNftHistory(newHistoryEntry);
                }
                else
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - ProcessTransferNFTAction", string.Format("History Between Two EOS Accounts (Unexpected), ActionSeq: {0}, Timestamp: {1}", action.account_action_seq, action.block_time));
                    continue;
                }

                // Check to make sure the nft is fully loaded
                NFTMetadata nftMetadata = _localDataManager.GetNftMetadataById(transferingNft.NFTMetadataId);
                await PopulateNFTAndNFTMetadata(transferingNft, nftMetadata);
            }
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

        private async Task PopulateBlkexplorerNFT(NFT newNft, NFTMetadata metadata)
        {
            if (newNft.FullyLoaded && metadata.FullyLoaded)
            {
                return;
            }

            dGood dGood = await GetDGoodFromTable(newNft.DGoodId);
            BlockExplorer blockExplorer = null;
            bool useBlockExplorer = false;

            try
            {
                if (!metadata.FullyLoaded || dGood == null)
                {
                    blockExplorer = await _uplandApiManager.GetBlockExplorersByDGoodId(newNft.DGoodId);
                    useBlockExplorer = true;
                }
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateBlkexplorerNFT", string.Format("Could Not Load BlockExplorer By DGoodId From Upland, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                return;
            }

            if (!newNft.FullyLoaded)
            {
                if (useBlockExplorer)
                {
                    newNft.SerialNumber = blockExplorer.Mint;
                }
                else
                {
                    newNft.SerialNumber = dGood.serial_number.Value;
                }

                newNft.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new BlockExplorerSpecificMetadata
                {
                    Link = @"https://play.upland.me/nft/block_explorer/nft-id/" + newNft.DGoodId
                });
                newNft.FullyLoaded = true;

                try
                {
                    _localDataManager.UpsertNft(newNft);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateBlkexplorerNFT", string.Format("Failed Saving BlockExplorer NFT, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }

            if (!metadata.FullyLoaded)
            {
                BlockExplorerMetadata currentMetadata = HelperFunctions.HelperFunctions.DecodeMetadata<BlockExplorerMetadata>(metadata.Metadata);
                currentMetadata.Image = blockExplorer.Image;
                currentMetadata.Description = blockExplorer.Description;
                currentMetadata.RarityLevel = blockExplorer.RarityLevel;
                metadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(currentMetadata);
                metadata.FullyLoaded = true;

                try
                {
                    _localDataManager.UpsertNftMetadata(metadata);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateBlkexplorerNFT", string.Format("Failed Saving BlockExplorer Metadata, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }
        }

        private async Task PopulateEssentialNFT(NFT newNft, NFTMetadata metadata)
        {
            if (newNft.FullyLoaded && metadata.FullyLoaded)
            {
                return;
            }

            dGood dGood = await GetDGoodFromTable(newNft.DGoodId);
            NFLPALegit legit = null;
            bool useLegit = false;

            try
            {
                if (!metadata.FullyLoaded || dGood == null)
                {
                    legit = await _uplandApiManager.GetNFLPALegitsByDGoodId(newNft.DGoodId);
                    useLegit = true;
                }
            }
            catch (Exception ex)
            {
                useLegit = false;
            }

            if (!newNft.FullyLoaded)
            {
                if (useLegit)
                {
                    newNft.SerialNumber = legit.Mint;
                }
                else
                {
                    newNft.SerialNumber = dGood.serial_number.Value;
                }

                string displayName = HelperFunctions.HelperFunctions.DecodeMetadata<EssentialMetadata>(metadata.Metadata).DisplayName;
                newNft.FullyLoaded = true;
                newNft.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new EssentialSpecificMetadata
                {
                    IsVariant = Regex.Match(displayName, " AWAY ").Success,
                    Link = @"https://play.upland.me/legit-preview/" + newNft.DGoodId
                });

                try
                {
                    _localDataManager.UpsertNft(newNft);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateEssentialNFT", string.Format("Failed Saving Essential NFT, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }

            if (!metadata.FullyLoaded && legit != null)
            {
                if (legit == null || legit.Position == null || legit.Position.Trim() == "")
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateEssentialNFT", string.Format("Missing Metadata, DGoodId: {0}", newNft.DGoodId));
                }

                MementoMetadata currentMetadata = HelperFunctions.HelperFunctions.DecodeMetadata<MementoMetadata>(metadata.Metadata);
                currentMetadata.Image = legit.Image;
                currentMetadata.TeamName = legit.TeamName;
                currentMetadata.FanPoints = legit.FanPoints;
                currentMetadata.Season = int.Parse(legit.Year);
                currentMetadata.ModelType = legit.LegitType;
                currentMetadata.PlayerFullName = legit.PlayerName;
                currentMetadata.PlayerPosition = legit.Position;
                metadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(currentMetadata);
                metadata.FullyLoaded = true;

                try
                {
                    _localDataManager.UpsertNftMetadata(metadata);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateEssentialNFT", string.Format("Failed Saving Essential Metadata, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }
        }

        private async Task PopulateMementoNFT(NFT newNft, NFTMetadata metadata)
        {
            if (newNft.FullyLoaded && metadata.FullyLoaded)
            {
                return;
            }

            NFLPALegit legit = null;
            NFLPALegitMintInfo legitMintInfo = null;

            try
            {
                legit = await _uplandApiManager.GetNFLPALegitsByDGoodId(newNft.DGoodId);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateMementoNFT", string.Format("Could Not Load Memento By DGoodId From Upland, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                return;
            }

            try
            {
                legitMintInfo = await _uplandApiManager.GetMementoMintInfo(legit.LegitId);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateMementoNFT", string.Format("Could Not Load Memento Mint Info By DGoodId From Upland, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                return;
            }

            if (!newNft.FullyLoaded)
            {
                newNft.SerialNumber = legit.Mint;
                newNft.FullyLoaded = true;
                newNft.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new MementoSpecificMetadata
                {
                    LegitId = legit.LegitId,
                    GameDate = legitMintInfo.Scene.GameDate,
                    OpponentTeam = legitMintInfo.Scene.OpponentTeamName,
                    HomeTeam = legitMintInfo.Scene.HomeTeamName,
                    MainStats = legitMintInfo.Stats.MainStats,
                    AdditionalStats = legitMintInfo.Stats.AdditionalStats,
                    Link = legit.Link
                });

                try
                {
                    _localDataManager.UpsertNft(newNft);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateMementoNFT", string.Format("Failed Saving Memento NFT, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }

            if (!metadata.FullyLoaded)
            {
                if (legit == null || legit.Position == null || legit.Position.Trim() == "")
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateMementoNFT", string.Format("Missing Metadata, DGoodId: {0}", newNft.DGoodId));
                    return;
                }
                MementoMetadata currentMetadata = HelperFunctions.HelperFunctions.DecodeMetadata<MementoMetadata>(metadata.Metadata);
                currentMetadata.Image = legit.Image;
                currentMetadata.TeamName = legit.TeamName;
                currentMetadata.FanPoints = legit.FanPoints;
                currentMetadata.Season = int.Parse(legit.Year);
                currentMetadata.ModelType = legit.LegitType;
                currentMetadata.PlayerFullName = legit.PlayerName;
                currentMetadata.PlayerPosition = legit.Position;
                metadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(currentMetadata);
                metadata.FullyLoaded = true;

                try
                {
                    _localDataManager.UpsertNftMetadata(metadata);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateMementoNFT", string.Format("Failed Saving Memento Metadata, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }
        }

        private async Task PopulateSpirithlwnNFT(NFT newNft, NFTMetadata metadata)
        {
            if (newNft.FullyLoaded && metadata.FullyLoaded)
            {
                return;
            }

            dGood dGood = await GetDGoodFromTable(newNft.DGoodId);
            SpiritLegit spiritLegit = null;
            bool useSpiritLegit = false;

            try
            {
                if (!metadata.FullyLoaded || dGood == null)
                {
                    spiritLegit = await _uplandApiManager.GetSpiritLegitsByDGoodId(newNft.DGoodId);
                    useSpiritLegit = true;
                }
            }
            catch (Exception ex)
            {
                useSpiritLegit = false;
            }

            if (!newNft.FullyLoaded)
            {
                if (useSpiritLegit)
                {
                    newNft.SerialNumber = spiritLegit.Mint;
                }
                else
                {
                    newNft.SerialNumber = dGood.serial_number.Value;
                }

                newNft.FullyLoaded = true;
                newNft.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new SpirithlwnSpecificMetadata
                {
                    Link = @"https://play.upland.me/nft-3d/spirit/" + newNft.DGoodId
                });

                try
                {
                    _localDataManager.UpsertNft(newNft);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateSpirithlwnNFT", string.Format("Failed Saving SpiritLegit NFT, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }

            if (!metadata.FullyLoaded && useSpiritLegit)
            {
                SpirithlwnMetadata currentMetadata = HelperFunctions.HelperFunctions.DecodeMetadata<SpirithlwnMetadata>(metadata.Metadata);
                currentMetadata.Image = spiritLegit.Image;
                currentMetadata.RarityLevel = spiritLegit.Rarity;
                metadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(currentMetadata);
                metadata.FullyLoaded = true;

                try
                {
                    _localDataManager.UpsertNftMetadata(metadata);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateSpirithlwnNFT", string.Format("Failed Saving SpiritLegit Metadata, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }
        }

        private async Task PopulateStructornmtNFT(NFT newNft, NFTMetadata metadata)
        {
            if (newNft.FullyLoaded && metadata.FullyLoaded)
            {
                return;
            }

            dGood dGood = await GetDGoodFromTable(newNft.DGoodId);
            Decoration decoration = null;
            bool useDecoration = false;

            try
            {
                if (!metadata.FullyLoaded || dGood == null)
                {
                    decoration = await _uplandApiManager.GetDecorationsByDGoodId(newNft.DGoodId);
                    useDecoration = true;
                }
            }
            catch (Exception ex)
            {
                useDecoration = false;
            }

            if (!newNft.FullyLoaded)
            {
                if (useDecoration)
                {
                    newNft.SerialNumber = decoration.Mint;
                }
                else
                {
                    newNft.SerialNumber = dGood.serial_number.Value;
                }

                newNft.FullyLoaded = true;
                newNft.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new StructornmtSpecificMetadata
                {
                    Link = @"https://play.upland.me/nft-3d/decoration/" + newNft.DGoodId
                });

                try
                {
                    _localDataManager.UpsertNft(newNft);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateStructornmtNFT", string.Format("Failed Saving Decoration NFT, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }

            if (!metadata.FullyLoaded && decoration != null)
            {
                StructornmtMetadata currentMetadata = HelperFunctions.HelperFunctions.DecodeMetadata<StructornmtMetadata>(metadata.Metadata);
                currentMetadata.Image = decoration.Image;
                currentMetadata.DecorationId = decoration.DecorationId;
                currentMetadata.RarityLevel = decoration.Rarity;
                currentMetadata.BuildingType = decoration.Subtitle;
                metadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(currentMetadata);
                metadata.FullyLoaded = true;

                try
                {
                    _localDataManager.UpsertNftMetadata(metadata);
                }
                catch (Exception ex)
                {
                    _localDataManager.CreateErrorLog("UplandNFTActSurfer.cs - PopulateStructornmtNFT", string.Format("Failed Saving Decoration Metadata, DGoodId: {0}, EX: {1}", newNft.DGoodId, ex.Message));
                    return;
                }
            }
        }

        private async Task<dGood> GetDGoodFromTable(int dGoodId)
        {
            // IN Prod Get the DGoods Individually
            return await _blockchainManager.GetDGoodFromTable(dGoodId);
            /*
            // FOR DEV LOADING ONLY
            if (_currentdGoodTable == null || _currentdGoodTable.Count == 0 || _dGoodTableExpirationDate < DateTime.UtcNow)
            {
                _currentdGoodTable = await _blockchainManager.GetAllDGoodsFromTable();
                _dGoodTableExpirationDate.AddDays(1);
            }

            return _currentdGoodTable.FirstOrDefault(d => d.id == dGoodId);
            */
        }

        private async Task PopulateNFTAndNFTMetadata(NFT nft, NFTMetadata metadata)
        {
            if (nft.FullyLoaded && metadata.FullyLoaded)
            {
                return;
            }

            switch (metadata.Category)
            {
                case "blkexplorer":
                    await PopulateBlkexplorerNFT(nft, metadata);
                    break;
                case "essential":
                    await PopulateEssentialNFT(nft, metadata);
                    break;
                case "memento":
                    await PopulateMementoNFT(nft, metadata);
                    break;
                case "spirithlwn":
                    await PopulateSpirithlwnNFT(nft, metadata);
                    break;
                case "structornmt":
                    await PopulateStructornmtNFT(nft, metadata);
                    break;
                case "structure":
                    break;
                default:
                    throw new Exception("Unknown NFT Category Detected Stoping Updates");
            }
        }
    }
}