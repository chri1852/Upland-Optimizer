using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Interfaces.Repositories;
using Upland.Interfaces.Managers;
using Upland.Types;
using Upland.Types.Types;

namespace Startup.Commands
{
    public class PremiumCommands : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;
        private readonly ILocalDataManager _localDataManager;
        private readonly IUplandApiRepository _uplandApiRepository;

        public PremiumCommands(ILocalDataManager localDataManager, IUplandApiRepository uplandApiRepository)
        {
            _localDataManager = localDataManager;
            _uplandApiRepository = uplandApiRepository;
            _random = new Random();
        }

        [Command("OptimizerLevelRun")]
        public async Task OptimizerLevelRun(int qualityLevel)
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredVerifiedAndPaid(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.Id);
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
                _localDataManager.DeleteOptimizerRuns(registeredUser.Id);
            }

            try
            {
                CollectionOptimizer optimizer = new CollectionOptimizer(_localDataManager, _uplandApiRepository);
                await ReplyAsync(string.Format("Got it {0}! I have started your level {1} optimization run.", HelperFunctions.GetRandomName(_random), qualityLevel));
                OptimizerRunRequest runRequest = new OptimizerRunRequest(registeredUser.UplandUsername.ToLower(), qualityLevel);
                await optimizer.RunAutoOptimization(registeredUser, runRequest);
                return;
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("PremiumCommands - OptimizerLevelRun", ex.Message);
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }
        }

        [Command("OptimizerWhatIfRun")]
        public async Task OptimizerWhatIfRun(int collectionId, int numberOfProps, double averageMonthlyUpx)
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredVerifiedAndPaid(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.Id);
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
                _localDataManager.DeleteOptimizerRuns(registeredUser.Id);
            }

            try
            {
                CollectionOptimizer optimizer = new CollectionOptimizer(_localDataManager, _uplandApiRepository);
                await ReplyAsync(string.Format("Bingo {0}! I have started your What If optimization run.", HelperFunctions.GetRandomName(_random)));
                OptimizerRunRequest runRequest = new OptimizerRunRequest(registeredUser.UplandUsername.ToLower(), collectionId, numberOfProps, averageMonthlyUpx);
                await optimizer.RunAutoOptimization(registeredUser, runRequest);

                return;
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("PremiumCommands - OptimizerWhatIfRun", ex.Message);
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }
        }

        [Command("OptimizerExcludeRun")]
        public async Task OptimizerExcludeRun(string collectionIds)
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredVerifiedAndPaid(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.Id);
            if (currentRun != null && currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("You alread have a run in progress {0}. Try using my !OptimizerStatus command to track its progress.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            List<int> excludeCollectionIds = new List<int>();
            foreach (string id in collectionIds.Split(","))
            {
                int collectionId = -1;
                if (int.TryParse(id, out collectionId))
                {
                    if (Consts.StandardCollectionIds.Contains(collectionId))
                    {
                        await ReplyAsync(string.Format("This command doesn't work on standard collections {0}.", HelperFunctions.GetRandomName(_random)));
                        return;
                    }

                    excludeCollectionIds.Add(collectionId);
                }
                else
                {
                    await ReplyAsync(string.Format("I don't think {0} is a number {1}.", id, HelperFunctions.GetRandomName(_random)));
                    return;
                }
            }

            if (currentRun != null)
            {
                _localDataManager.DeleteOptimizerRuns(registeredUser.Id);
            }

            try
            {
                CollectionOptimizer optimizer = new CollectionOptimizer(_localDataManager, _uplandApiRepository);
                await ReplyAsync(string.Format("Bingo {0}! I have started your exclude optimization run.", HelperFunctions.GetRandomName(_random)));
                OptimizerRunRequest runRequest = new OptimizerRunRequest(registeredUser.UplandUsername.ToLower(), excludeCollectionIds);
                await optimizer.RunAutoOptimization(registeredUser, runRequest);

                return;
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("PremiumCommands - OptimizerExcludeRun", ex.Message);
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

            if (!registeredUser.DiscordVerified)
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
