using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.BlockchainTypes;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class BlockchainSendFinder : IBlockchainSendFinder
    {
        private readonly ILocalDataManager _localDataManager;
        private readonly IBlockchainManager _blockchainManager;
        private List<string> propertyIdsToWatch;
        private bool isProcessing;

        public BlockchainSendFinder(ILocalDataManager localDataManager, IBlockchainManager blockchainManager)
        {
            _localDataManager = localDataManager;
            _blockchainManager = blockchainManager;
            propertyIdsToWatch = localDataManager.GetConfigurationValue(Consts.CONFIG_PROPIDSTOMONITORFORSENDS).Split(",").ToList();
            isProcessing = false;
        }

        public async Task RunBlockChainUpdate()
        {
            DateTime lastDateProcessed = DateTime.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXSENDTIMESTAMPPROCESSED));

            try
            {
                await SearchSendsFromDate(lastDateProcessed);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("BlockchainSendFinder.cs - RunBlockChainUpdate", ex.Message);
                this.isProcessing = false;
            }
        }

        public async Task SearchSendsFromDate(DateTime startDate)
        {
            bool enableUpdates = bool.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES));

            if (!enableUpdates || this.isProcessing)
            {
                return;
            }

            List<Tuple<decimal, string, string>> registeredUserEOSAccounts = _localDataManager.GetRegisteredUsersEOSAccounts();

            this.isProcessing = true;

            int minutesToMoveFoward = 60; // One Hours
            bool continueLoad = true;

            while (continueLoad)
            {
                List<HistoryAction> actions = new List<HistoryAction>();

                // Have to do this in a retry loop due to timeouts
                bool retry = true;
                while (retry)
                {
                    try
                    {
                        Thread.Sleep(2000);
                        actions = await _blockchainManager.GetSendActionsFromTime(startDate, minutesToMoveFoward);
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
                        _localDataManager.CreateErrorLog("BlockchainSendFinder.cs - SearchSendsFromDate - Loop", ex.Message);
                        Thread.Sleep(5000);
                    }
                }

                if (actions.Count == 0)
                {
                    startDate = startDate.AddMinutes(minutesToMoveFoward);
                }
                else
                {
                    actions = actions.OrderBy(a => a.timestamp).ToList();
                    ProcessActions(actions, registeredUserEOSAccounts);

                    if (actions.Count < 1000)
                    {
                        startDate = startDate.AddMinutes(minutesToMoveFoward);
                    }
                    else
                    {
                        startDate = actions.Max(a => a.timestamp);
                    }
                }

                if (startDate >= DateTime.UtcNow)
                {
                    continueLoad = false;
                }
            }

            this.isProcessing = false;
        }

        public void ProcessActions(List<HistoryAction> actions, List<Tuple<decimal, string, string>> registeredUserEOSAccounts)
        {
            DateTime maxTimestampProcessed = DateTime.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_MAXSENDTIMESTAMPPROCESSED));

            foreach (HistoryAction action in actions)
            {
                if (action.timestamp < maxTimestampProcessed)
                {
                    // We've already processed this event
                    continue;
                }

                if (action.act.name == "n41")
                {
                    if (registeredUserEOSAccounts.Any(e => e.Item3 == action.act.data.p51) && propertyIdsToWatch.Any(p => p == action.act.data.a45))
                    {
                        try
                        {
                            string uplandUsername = registeredUserEOSAccounts.Where(e => e.Item3 == action.act.data.p51).First().Item2;
                            RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);
                            registeredUser.SendUPX += int.Parse(action.act.data.p54.Split(".00 UP")[0]);
                            _localDataManager.UpdateRegisteredUser(registeredUser);
                        }
                        catch (Exception ex)
                        {
                            _localDataManager.CreateErrorLog("BlockchainSendFinder - ProcessActions", string.Format("Failed Adding UPX, a45: {0}, p51: {1}, p54: {2}, ex: {3}", action.act.data.a45, action.act.data.p51, action.act.data.p54, ex.Message));
                        }
                    }
                }
                else if (action.act.name == "n111")
                {
                    if (action.act.data.p2 != null && action.act.data.p2 == Consts.HornbrodEOSAccount && registeredUserEOSAccounts.Any(e => e.Item3 == action.act.data.p1))
                    {
                        try
                        {
                            string uplandUsername = registeredUserEOSAccounts.Where(e => e.Item3 == action.act.data.p1).First().Item2;
                            RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);
                            registeredUser.SendUPX += int.Parse(action.act.data.p45.Split(".00 UP")[0]) - (int)Math.Floor(double.Parse(action.act.data.p134.Split("UP")[0]));
                            _localDataManager.UpdateRegisteredUser(registeredUser);
                        }
                        catch (Exception ex)
                        {
                            _localDataManager.CreateErrorLog("BlockchainSendFinder - Process Send UPX", string.Format("Failed Adding UPX, p1: {0}, p133: {1}, p134: {2}, p2: {3}, p45: {4}, ex: {5}", action.act.data.p1, action.act.data.p133, action.act.data.p134, action.act.data.p2, action.act.data.p45, ex.Message));
                        }
                    }
                    else if (action.act.data.data.p2 != null && action.act.data.data.p2 == Consts.HornbrodEOSAccount && registeredUserEOSAccounts.Any(e => e.Item3 == action.act.data.data.p1))
                    {
                        try
                        {
                            string uplandUsername = registeredUserEOSAccounts.Where(e => e.Item3 == action.act.data.data.p1).First().Item2;
                            RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);
                            registeredUser.SendUPX += int.Parse(action.act.data.data.p45.Split(".00 UP")[0]) - (int)Math.Floor(double.Parse(action.act.data.data.p134.Split("UP")[0]));
                            _localDataManager.UpdateRegisteredUser(registeredUser);
                        }
                        catch (Exception ex)
                        {
                            _localDataManager.CreateErrorLog("BlockchainSendFinder - Process Send UPX", string.Format("Failed Adding UPX, p1: {0}, p133: {1}, p134: {2}, p2: {3}, p45: {4}, ex: {5}", action.act.data.p1, action.act.data.p133, action.act.data.p134, action.act.data.p2, action.act.data.p45, ex.Message));
                        }
                    }
                }
            }

            if (actions.Max(a => a.timestamp) > maxTimestampProcessed)
            {
                maxTimestampProcessed = actions.Max(a => a.timestamp);
                _localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXSENDTIMESTAMPPROCESSED, maxTimestampProcessed.ToString());
            }
        }
    }
}
