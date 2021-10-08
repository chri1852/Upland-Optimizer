using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.Types;

namespace Startup.Commands
{
    public class PremiumCommands : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;

        public PremiumCommands()
        {
            _random = new Random();
        }

        [Command("OptimizerLevelRun")]
        public async Task OptimizerRun(int qualityLevel)
        {
            LocalDataManager localDataManager = new LocalDataManager();

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredVerifiedAndPaid(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = localDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);
            if (currentRun != null && currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("You alread have a run in progress {0}. Try using my !OptimizerStatus command to track its progress.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (qualityLevel < 3)
            {
                await ReplyAsync(string.Format("Thats a bit too low {0}! The lowest I can go is 3.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (qualityLevel > 10)
            {
                await ReplyAsync(string.Format("Whoa there {0}! This quality level is a bit too much for me, the best I can do is 10 and it takes a long time.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (currentRun != null)
            {
                localDataManager.DeleteOptimizerRuns(registeredUser.DiscordUserId);
            }

            try
            {
                Task child = Task.Factory.StartNew(async () =>
                {
                    CollectionOptimizer optimizer = new CollectionOptimizer();
                    await optimizer.RunAutoOptimization(registeredUser, qualityLevel);
                });

                await ReplyAsync(string.Format("Got it {0}! I have started your level {1} optimization run.", HelperFunctions.GetRandomName(_random), qualityLevel));
                return;
            }
            catch
            {
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }
        }

        private async Task<bool> EnsureRegisteredVerifiedAndPaid(RegisteredUser registeredUser)
        {
            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("You don't appear to exist {0}. Try again with my !RegisterMe command with your Upland username!", HelperFunctions.GetRandomName(_random)));
                return false;
            }

            if (!registeredUser.Verified)
            {
                await ReplyAsync(string.Format("Looks like you are not verified {0}. Try Again with my !VerifyMe command.", HelperFunctions.GetRandomName(_random)));
                return false;
            }

            if (!registeredUser.Paid)
            {
                await ReplyAsync(string.Format("Looks like you are not a supporter {0}. To become one call my !SupportMe command.", HelperFunctions.GetRandomName(_random)));
                return false;
            }

            return true;
        }
    }
}
