using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.Types;

namespace Startup.Commands
{
    public class AdminCommands : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;

        public AdminCommands()
        {
            _random = new Random();
        }

        private async Task<bool> checkIfAdmin(ulong discordUserId)
        {
            if(discordUserId != Consts.AdminDiscordId)
            {
                await ReplyAsync(string.Format("You are not an admin {0}.", HelperFunctions.GetRandomName(_random)));
                return false;
            }
            return true;
        }

        [Command("AdminOptimizerRun")]
        public async Task OptimizerRun(string uplandUsername)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            LocalDataManager localDataManager = new LocalDataManager();

            OptimizationRun currentRun = localDataManager.GetLatestOptimizationRun(Consts.TestUserDiscordId);
            if (currentRun != null && currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("Test User Run is in Progress, Please Wait."));
                return;
            }

            if (currentRun != null)
            {
                localDataManager.DeleteOptimizerRuns(Consts.TestUserDiscordId);
            }

            try
            {
                Task child = Task.Factory.StartNew(async () =>
                {
                    CollectionOptimizer optimizer = new CollectionOptimizer();
                    await optimizer.RunAutoOptimization(new RegisteredUser
                    {
                        DiscordUserId = Consts.TestUserDiscordId,
                        DiscordUsername = "TEST_USER_NAME",
                        UplandUsername = uplandUsername
                    },
                    7);
                });

                await ReplyAsync(string.Format("Test Run Has Started For: {0}", uplandUsername));
                return;
            }
            catch
            {
                await ReplyAsync(string.Format("Test Run Has Failed For: {0}", uplandUsername));
                return;
            }
        }

        [Command("AdminOptimizerResults")]
        public async Task OptimizerResults()
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            LocalDataManager localDataManager = new LocalDataManager();
            OptimizationRun currentRun = localDataManager.GetLatestOptimizationRun(Consts.TestUserDiscordId);

            if (currentRun.Status == Consts.RunStatusCompleted)
            {
                List<string> results = Encoding.UTF8.GetString(currentRun.Results).Split(Environment.NewLine).ToList();
                string message = "";
                foreach (string entry in results)
                {
                    if (message.Length + entry.Length + 1 < 2000)
                    {
                        message += entry;
                        message += Environment.NewLine;
                    }
                    else
                    {
                        await ReplyAsync(string.Format("{0}", message));
                        message = entry;
                        message += Environment.NewLine;
                    }
                }

                await ReplyAsync(string.Format("{0}", message));

                return;
            }
        }

        [Command("AdminPopulateCollections")]
        public async Task AdminPopulateCollections()
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            LocalDataManager localDataManager = new LocalDataManager();

            try
            {
                Task child = Task.Factory.StartNew(async () =>
                {
                    await localDataManager.PopulateDatabaseCollectionInfo();
                });
                await ReplyAsync(string.Format("Running Collection Update in Child Task"));
            }
            catch (Exception ex)
            {
                await ReplyAsync(string.Format("Update Failed: {0}", ex.Message));
            }
        }

        [Command("AdminPopulateCollectionProperty")]
        public async Task AdminPopulateCollectionProperty(int collectionId)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            LocalDataManager localDataManager = new LocalDataManager();
            List<Collection> collections = localDataManager.GetCollections();

            if (!collections.Any(c => c.Id == collectionId))
            {
                await ReplyAsync(string.Format("No Collection found for CollectionId {0}.", collectionId));
                return;
            }

            try
            {
                Task child = Task.Factory.StartNew(async () =>
                {
                    await localDataManager.PopulationCollectionPropertyData(collectionId);
                });
                await ReplyAsync(string.Format("Running Collection Property Update in Child Task for CollectionId {0}.", collectionId));
            }
            catch (Exception ex)
            {
                await ReplyAsync(string.Format("Update Failed: {0}", ex.Message));
            }
        }
    }
}
