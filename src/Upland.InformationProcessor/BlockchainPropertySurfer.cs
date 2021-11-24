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
            DateTime startDate = new DateTime(2019, 06, 01, 00, 00, 00);

            BuildBlockChainFromDate(startDate);
        }

        public async Task BuildBlockChainFromDate(DateTime startDate)
        {

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
                    actions = await blockchainManager.GetPropertyActionsFromTime(startDate, minutesToMoveFoward);
                    if (actions != null)
                    {
                        retry = false;
                    }
                }

                if (actions.Count == 0)
                {
                    startDate = startDate.AddMinutes(minutesToMoveFoward);
                }
                else
                {
                    ProcessActions(actions, maxGlobalSequence);

                    maxGlobalSequence = actions.Max(a => a.global_sequence);
                    if (actions.Count < 1000)
                    {
                        startDate = startDate.AddMinutes(minutesToMoveFoward);
                    }
                    else
                    {
                        startDate = actions.Max(a => a.timestamp);
                    }
                }

                if (startDate >= DateTime.Now)
                {
                    continueLoad = false;
                }
            }

            // Update the Max Global Sequence seen, this ensures we don't process the same event twice
            //localDataManager.UpsertConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE, maxGlobalSequence.ToString());
        }

        private void ProcessActions(List<HistoryAction> actions, long maxGlobalSequence)
        {
            foreach(HistoryAction action in actions)
            {
                if (action.global_sequence < maxGlobalSequence )
                {
                    // We've already processed this event
                    continue;
                }

                switch(action.act.name)
                {
                    case "a4":
                        ProcessMintingAction(action);
                        break;
                    case "n12":
                        ProcessOfferAction(action);
                        break;
                    case "n13":
                        ProcessOfferResolutionAction(action);
                        break;
                    case "n5":
                        ProcessPurchaseAction(action);
                        break;
                    case "n2":
                        ProcessPlaceForSaleAction(action);
                        break;
                    case "n4":
                        ProcessRemoveFromSaleAction(action);
                        break;
                    case "n52":
                        ProcessButForFiatAction(action);
                        break;
                    default:
                        continue;
                }
            }
        }

        private void ProcessButForFiatAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }

        private void ProcessRemoveFromSaleAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }

        private void ProcessPlaceForSaleAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }

        private void ProcessPurchaseAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }

        private void ProcessOfferResolutionAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }

        private void ProcessOfferAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }

        private void ProcessMintingAction(HistoryAction action)
        {
            throw new NotImplementedException();
        }
    }
}
