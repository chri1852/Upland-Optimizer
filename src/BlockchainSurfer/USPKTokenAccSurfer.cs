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
    public class USPKTokenAccSurfer : IUSPKTokenAccSurfer
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IBlockchainManager _blockchainManager;
        private readonly string _uspktokenacc;
        private readonly string _playuplandme;

        private List<string> _loadedStructureTypes;
        private bool _isProcessing;

        public USPKTokenAccSurfer(ILocalDataManager localDataManager, IBlockchainManager blockchainManager)
        {
            _localDataManager = localDataManager;
            _blockchainManager = blockchainManager;
            _uspktokenacc = "uspktokenacc";
            _playuplandme = "playuplandme";

            _isProcessing = false;
            _loadedStructureTypes = new List<string>();
        }

        public async Task RunBlockChainUpdate()
        {
            long lastActionProcessed = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUSPKTOKENACCACTIONSEQNUM));

            try
            {
                await ProcessBlockchainFromAction(lastActionProcessed);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - RunBlockChainUpdate", ex.Message);
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
                List<UspkTokenAccAction> actions = new List<UspkTokenAccAction>();

                // Have to do this in a retry loop due to timeouts
                bool retry = true;
                while (retry)
                {
                    try
                    {
                        Thread.Sleep(2000);
                        actions = (await _blockchainManager.GetEOSFlareActions<GetUspkTokenAccActionsResponse>(lastActionProcessed + 1, _uspktokenacc)).actions;
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
                        _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - BuildBlockChainFromDate - Loop", ex.Message);
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
                        _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessActions - Exception Bubbled Up Disable Blockchain Updates", ex.Message);
                        _localDataManager.UpsertConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES, false.ToString());
                    }

                    lastActionProcessed = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUSPKTOKENACCACTIONSEQNUM));
                }
            }

            _isProcessing = false;
        }

        private async Task ProcessActions(List<UspkTokenAccAction> actions)
        {
            long maxActionSeqNum = long.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXUSPKTOKENACCACTIONSEQNUM));
            foreach (UspkTokenAccAction action in actions)
            {
                if (action.account_action_seq < maxActionSeqNum)
                {
                    // We've already processed this event
                    _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessActions", string.Format("Skipping Action {0} < {1}", action.account_action_seq, maxActionSeqNum));
                    continue;
                }

                // DEBUG
                if (action.block_time > new DateTime(2021, 6, 20))
                {
                    continue;
                }

                switch (action.action_trace.act.name)
                {
                    case "transfer":
                        await ProcessTransferAction(action);
                        break;
                }

                if (action.account_action_seq > maxActionSeqNum)
                {
                    maxActionSeqNum = action.account_action_seq;
                    _localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXUSPKTOKENACCACTIONSEQNUM, action.account_action_seq.ToString());
                }
            }
        }

        private async Task ProcessTransferAction(UspkTokenAccAction action)
        {
            if (action.action_trace.act.data.to == null || action.action_trace.act.data.from == null || action.action_trace.act.data.quantity == null)
            {
                _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessTransferAction", string.Format("Trx_id: {1}", action.action_trace.trx_id));
                return;
            }

            decimal amount;
            if (!decimal.TryParse(action.action_trace.act.data.quantity.Split(" USPK")[0], out amount))
            {
                _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessTransferAction - Could Not Parse Amount", string.Format("To: {0}, From: {1}, Quantity: {2}, Memo: {3} Trx_id: {4}", action.action_trace.act.data.to, action.action_trace.act.data.from, action.action_trace.act.data.quantity, action.action_trace.act.data.memo, action.action_trace.trx_id));
                return;
            }

            if (action.action_trace.act.data.to == _playuplandme)
            {
                EOSUser user = _localDataManager.GetUplandUsernameByEOSAccount(action.action_trace.act.data.from);

                if (user == null)
                {
                    user = new EOSUser
                    {
                        EOSAccount = action.action_trace.act.data.a54,
                        UplandUsername = "",
                        Joined = action.block_time,
                        Spark = 0
                    };
                }

                if (Regex.Match(action.action_trace.act.data.memo, "^STAKE,").Success)
                {
                    List<SparkStaking> stakedSpark = _localDataManager.GetSparkStakingByEOSUserId(user.Id);
                    int dGoodId = int.Parse(action.action_trace.act.data.memo.Split("STAKE,")[1]);

                    SparkStaking stake = stakedSpark.Where(s => s.End == null && s.DGoodId == dGoodId).FirstOrDefault();

                    if (stake == null)
                    {
                        _localDataManager.UpsertSparkStaking(new SparkStaking
                        {
                            Id = -1,
                            DGoodId = dGoodId,
                            EOSUserId = user.Id,
                            Amount = amount,
                            Start = action.block_time,
                            End = null
                        });
                    }
                    else
                    {
                        stake.End = action.block_time;
                        _localDataManager.UpsertSparkStaking(stake);

                        _localDataManager.UpsertSparkStaking(new SparkStaking
                        {
                            Id = -1,
                            DGoodId = dGoodId,
                            EOSUserId = user.Id,
                            Amount = stake.Amount + amount,
                            Start = action.block_time,
                            End = null
                        });
                    }
                }
                else if (Regex.Match(action.action_trace.act.data.memo, "^BUILD,").Success)
                {
                    int dGoodId = int.Parse(action.action_trace.act.data.memo.Split("BUILD,")[1].Split(",")[0]);
                    _localDataManager.UpsertSparkStaking(new SparkStaking
                    {
                        Id = -1,
                        DGoodId = dGoodId,
                        EOSUserId = user.Id,
                        Amount = amount,
                        Start = action.block_time,
                        End = null
                    });

                    HandleStructureDGoodCreation(dGoodId, action);
                }
                else
                {
                    _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessTransferAction - Unknown Memo String", string.Format("To: {0}, From: {1}, Quantity: {2}, Memo: {3} Trx_id: {4}", action.action_trace.act.data.to, action.action_trace.act.data.from, action.action_trace.act.data.quantity, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    return;
                }

                user.Spark -= amount;
                _localDataManager.UpsertEOSUser(user);

                return;
            }

            if (action.action_trace.act.data.from == _playuplandme)
            {
                EOSUser user = _localDataManager.GetUplandUsernameByEOSAccount(action.action_trace.act.data.to);

                if (user == null)
                {
                    user = new EOSUser
                    {
                        EOSAccount = action.action_trace.act.data.a54,
                        UplandUsername = "",
                        Joined = action.block_time,
                        Spark = 0
                    };
                }

                if (action.action_trace.account_ram_deltas != null && action.action_trace.account_ram_deltas.Count > 0 && action.action_trace.account_ram_deltas.First().account == _uspktokenacc)
                {
                    // Issued Spark
                    user.Spark += amount;
                    _localDataManager.UpsertEOSUser(user);
                    return;
                }

                List<SparkStaking> stakedSpark = _localDataManager.GetSparkStakingByEOSUserId(user.Id);
                UspkTokenAccTransactionEntry transaction = await _blockchainManager.GetSingleTransactionById<UspkTokenAccTransactionEntry>(action.action_trace.trx_id);

                if ((stakedSpark == null 
                    || stakedSpark.Count == 0
                    || stakedSpark.All(s => s.End != null))
                    && transaction.traces.Any(t => t.act.name == "n512" || t.act.name == "n511" || t.act.name == "a32"))
                {
                    _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessTransferAction - No Spark Staked", string.Format("To: {0}, From: {1}, Quantity: {2}, Memo: {3} Trx_id: {4}", action.action_trace.act.data.to, action.action_trace.act.data.from, action.action_trace.act.data.quantity, action.action_trace.act.data.memo, action.action_trace.trx_id));
                    return;
                }

                if (transaction.traces.Any(t => t.act.name == "n512"))
                {
                    UspkTokenAccActionEntry playuplandmeAction = transaction.traces.Where(t => t.act.name == "n512").First().act;
                    SparkStaking stake = stakedSpark.Where(s => s.End == null && s.DGoodId == int.Parse(playuplandmeAction.data.p113)).FirstOrDefault();

                    if (stake == null)
                    {
                        _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessTransferAction - Could Not Find Stake For n512", string.Format("To: {0}, From: {1}, Quantity: {2}, Memo: {3} Trx_id: {4}", action.action_trace.act.data.to, action.action_trace.act.data.from, action.action_trace.act.data.quantity, action.action_trace.act.data.memo, action.action_trace.trx_id));
                        return;
                    }

                    if ((stake.Amount - amount) > 0)
                    {
                        _localDataManager.UpsertSparkStaking(new SparkStaking
                        {
                            Id = -1,
                            DGoodId = stake.DGoodId,
                            EOSUserId = stake.EOSUserId,
                            Amount = stake.Amount - amount,
                            Start = action.block_time,
                            End = null
                        });
                    }

                    stake.End = action.block_time;
                    _localDataManager.UpsertSparkStaking(stake);

                    user.Spark += amount;
                    _localDataManager.UpsertEOSUser(user);
                }
                else if (transaction.traces.Any(t => t.act.name == "n511"))
                {
                    UspkTokenAccActionEntry playuplandmeAction = transaction.traces.Where(t => t.act.name == "n511").First().act;
                    SparkStaking stake = stakedSpark.Where(s => s.End == null && s.DGoodId == int.Parse(playuplandmeAction.data.p113)).FirstOrDefault();

                    if (stake == null)
                    {
                        _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessTransferAction - Could Not Find Stake For n511", string.Format("To: {0}, From: {1}, Quantity: {2}, Memo: {3} Trx_id: {4}", action.action_trace.act.data.to, action.action_trace.act.data.from, action.action_trace.act.data.quantity, action.action_trace.act.data.memo, action.action_trace.trx_id));
                        return;
                    }

                    stake.End = action.block_time;
                    _localDataManager.UpsertSparkStaking(stake);

                    user.Spark += amount;
                    _localDataManager.UpsertEOSUser(user);
                }
                else if (transaction.traces.Any(t => t.act.name == "a32"))
                {
                    UspkTokenAccActionEntry playuplandmeAction = transaction.traces.Where(t => t.act.name == "a32").First().act;
                    SparkStaking stake = stakedSpark.Where(s => s.End == null && s.DGoodId == int.Parse(playuplandmeAction.data.p115.First())).FirstOrDefault();

                    if (stake == null)
                    {
                        _localDataManager.CreateErrorLog("USPKTokenAccSurfer.cs - ProcessTransferAction - Could Not Find Stake For a32", string.Format("To: {0}, From: {1}, Quantity: {2}, Memo: {3} Trx_id: {4}", action.action_trace.act.data.to, action.action_trace.act.data.from, action.action_trace.act.data.quantity, action.action_trace.act.data.memo, action.action_trace.trx_id));
                        return;
                    }

                    stake.End = action.block_time;
                    _localDataManager.UpsertSparkStaking(stake);

                    user.Spark += amount;
                    _localDataManager.UpsertEOSUser(user);
                }
                else
                {
                    // Issued Spark
                    user.Spark += amount;
                    _localDataManager.UpsertEOSUser(user);
                }
            }
        }

        private void HandleStructureDGoodCreation(int dGoodId, UspkTokenAccAction action)
        {
            long propertyId = long.Parse(action.action_trace.act.data.memo.Split(string.Format("BUILD,{0},", dGoodId))[1].Split(",")[0]);
            string category = "structure";
            string name = action.action_trace.act.data.memo.Split(",")[9];

            NFTMetadata structureMetadata = _localDataManager.GetNftMetadataByNameAndCategory(name, category);
            NFT structureNFT = _localDataManager.GetNftByDGoodId(dGoodId);

            if (structureMetadata == null)
            {
                // TODO ONLY FOR TESTING THE SPK ACCT
                structureMetadata = new NFTMetadata
                {
                    Id = -1,
                    Name = action.action_trace.act.data.memo.Split(",")[9],
                    Category = action.action_trace.act.data.memo.Split(",")[8],
                    Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(new StructureMetadata
                    {
                        SparkHours = int.Parse(action.action_trace.act.data.memo.Split(",")[5]),
                        MinimumSpark = decimal.Parse(action.action_trace.act.data.memo.Split(",")[6]) / 100,
                        MaximumSpark = decimal.Parse(action.action_trace.act.data.memo.Split(",")[7]) / 100
                    })
                };

                _loadedStructureTypes.Add(name);

                _localDataManager.UpsertNftMetadata(structureMetadata);
            }
            else
            {
                if (!_loadedStructureTypes.Contains(name))
                {
                    StructureMetadata metadata = HelperFunctions.HelperFunctions.DecodeMetadata<StructureMetadata>(structureMetadata.Metadata);
                    metadata.SparkHours = int.Parse(action.action_trace.act.data.memo.Split(",")[5]);
                    metadata.MinimumSpark = decimal.Parse(action.action_trace.act.data.memo.Split(",")[6]) / 100;
                    metadata.MaximumSpark = decimal.Parse(action.action_trace.act.data.memo.Split(",")[7]) / 100;
                    structureMetadata.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata(metadata);

                    _loadedStructureTypes.Add(name);

                    _localDataManager.UpsertNftMetadata(structureMetadata);
                }
            }

            if (structureNFT == null)
            {
                // This Should Happen rarely, the uplandnftact scrapper should find and create these
                structureNFT = new NFT
                {
                    DGoodId = dGoodId,
                    NFTMetadataId = 0, // This will get set by the uplandnftact scrapper
                    SerialNumber = 0, // No serial number on structures
                    Burned = false,
                    CreatedOn = action.block_time,
                    Metadata = HelperFunctions.HelperFunctions.EncodeMetadata<StructureSpecificMetaData>(new StructureSpecificMetaData
                    {
                        PropertyId = propertyId,
                    })
                };

                _localDataManager.UpsertNft(structureNFT);
            }
            else
            {
                // Only set the metadata, the rest has been handled by the uplandnftact scrapper
                structureNFT.Metadata = HelperFunctions.HelperFunctions.EncodeMetadata<StructureSpecificMetaData>(new StructureSpecificMetaData
                {
                    PropertyId = propertyId,
                });

                _localDataManager.UpsertNft(structureNFT);
            }
        }
    }
}