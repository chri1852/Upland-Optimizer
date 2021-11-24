using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Upland.Infrastructure.Blockchain;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.BlockchainTypes;

namespace Upland.InformationProcessor
{
    public class BlockchainPropertySurfer
    {
        private readonly LocalDataManager localDataManager;
        private readonly BlockchainManager blockchainManager;

        public BlockchainPropertySurfer()
        {
            localDataManager = new LocalDataManager();
            blockchainManager = new BlockchainManager();
        }

        public async Task BuildBlockChainFromBegining()
        {
            // Upland went live on the blockchain on 2019-06-06 11:51:37
            DateTime loopTime = new DateTime(2019, 06, 01, 00, 00, 00);

            // Advance a day at a time, unless there are more props
            int minutesToMoveFoward = 1440;
            bool continueLoad = true;
            long maxGlobalSequence = long.Parse(localDataManager.GetConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE));

            while(continueLoad)
            {
                List<HistoryAction> actions = new List<HistoryAction>();

                // Have to do this in a retry loop due to timeouts
                bool retry = true;
                while (retry)
                {
                    Thread.Sleep(2000);
                    actions = await blockchainManager.GetPropertyActionsFromTime(loopTime, minutesToMoveFoward);
                    if (actions != null)
                    {
                        retry = false;
                    }
                }

                if (actions.Count == 0)
                {
                    loopTime = loopTime.AddMinutes(minutesToMoveFoward);
                }
                else
                {
                    await ProcessActions(actions, maxGlobalSequence);

                    maxGlobalSequence = actions.Max(a => a.global_sequence);
                    if (actions.Count < 1000)
                    {
                        loopTime = loopTime.AddMinutes(minutesToMoveFoward);
                    }
                    else
                    {
                        loopTime = actions.Max(a => a.timestamp);
                    }
                }

                if (loopTime >= DateTime.Now)
                {
                    continueLoad = false;
                }
            }

            // Update the Max Global Sequence seen, this ensures we don't process the same event twice
            //localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE, maxGlobalSequence.ToString());
        }

        private async Task ProcessActions(List<HistoryAction> actions, long maxGlobalSequence)
        {
            foreach(HistoryAction action in actions)
            {
                if (action.global_sequence < maxGlobalSequence )
                {
                    // We've already processed this event
                    continue;
                }


            }
        }
    }
}
