using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            int minutesToMoveFoward = 60;
            bool continueLoad = true;
            long maxGlobalSequence = long.Parse(localDataManager.GetConfigurationValue(Consts.CONFIG_MAXGLOBALSEQUENCE));

            while(continueLoad)
            {
                List<HistoryAction> actions =  await blockchainManager.GetPropertyActionsFromTime(loopTime, minutesToMoveFoward);

                if (actions.Count == 0)
                {
                    loopTime = loopTime.AddMinutes(minutesToMoveFoward);
                }
                else
                {
                    ProcessActions(actions, maxGlobalSequence);

                    maxGlobalSequence = actions.Max(a => a.global_sequence);
                    loopTime = actions.Max(a => a.timestamp);
                }

                if (loopTime >= DateTime.Now)
                {
                    continueLoad = false;
                }
            }
        }

        private async Task ProcessActions(List<HistoryAction> actions, long maxGlobalSequence)
        {
            
        }
    }
}
