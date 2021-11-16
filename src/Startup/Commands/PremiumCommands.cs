using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.InformationProcessor;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.Types;

namespace Startup.Commands
{
    public class PremiumCommands : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;
        private readonly InformationProcessor _informationProcessor;

        public PremiumCommands(InformationProcessor informationProcessor)
        {
            _informationProcessor = informationProcessor;
            _random = new Random();
        }

        [Command("OptimizerLevelRun")]
        public async Task OptimizerLevelRun(int qualityLevel)
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
                CollectionOptimizer optimizer = new CollectionOptimizer();
                await ReplyAsync(string.Format("Got it {0}! I have started your level {1} optimization run.", HelperFunctions.GetRandomName(_random), qualityLevel));

                await optimizer.RunAutoOptimization(registeredUser, qualityLevel);
                return;
            }
            catch
            {
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }
        }

        [Command("OptimizerWhatIfRun")]
        public async Task OptimizerWhatIfRun(int collectionId, int numberOfProps, double averageMonthlyUpx)
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

            if (Consts.StandardCollectionIds.Contains(collectionId))
            {
                await ReplyAsync(string.Format("This command doesn't work on standard collections {0}.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (currentRun != null)
            {
                localDataManager.DeleteOptimizerRuns(registeredUser.DiscordUserId);
            }

            try
            {
                CollectionOptimizer optimizer = new CollectionOptimizer();
                await ReplyAsync(string.Format("Bingo {0}! I have started your level What If optimization run.", HelperFunctions.GetRandomName(_random)));

                await optimizer.RunAutoOptimization(registeredUser, 7, collectionId, numberOfProps, averageMonthlyUpx);

                return;
            }
            catch
            {
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }
        }

        [Command("ClearSalesCache")]
        public async Task ClearSalesCache()
        {
            LocalDataManager localDataManager = new LocalDataManager();

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredVerifiedAndPaid(registeredUser))
            {
                return;
            }

            try
            {
                _informationProcessor.ClearSalesCache();
                await ReplyAsync(string.Format("I Cleared out that moldy old sales data for you {0}", HelperFunctions.GetRandomName(_random)));
            }
            catch
            {
                await ReplyAsync(string.Format("I failed somehow, Grombrindal should probably hear of this."));
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
