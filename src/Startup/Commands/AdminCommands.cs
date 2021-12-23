using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.InformationProcessor;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.Types;

namespace Startup.Commands
{
    public class AdminCommands : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;
        private readonly InformationProcessor _informationProcessor;
        private readonly LocalDataManager _localDataManager;

        public AdminCommands(InformationProcessor informationProcessor, LocalDataManager localDataManager)
        {
            _random = new Random();
            _informationProcessor = informationProcessor;
            _localDataManager = localDataManager;
        }

        private async Task<bool> checkIfAdmin(ulong discordUserId)
        {
            ulong adminId = ulong.Parse(JsonSerializer.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(@"appsettings.json"))["AdminDiscordId"]);

            if (discordUserId != adminId)
            {
                await ReplyAsync(string.Format("You are not an admin {0}.", HelperFunctions.GetRandomName(_random)));
                return false;
            }
            return true;
        }

        [Command("AdminOptimizerRun")]
        public async Task AdminOptimizerRun(string uplandUsername, int qualityLevel)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(Consts.TestUserDiscordId);
            if (currentRun != null && currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("Test User Run is in Progress, Please Wait."));
                return;
            }

            if (currentRun != null)
            {
                _localDataManager.DeleteOptimizerRuns(Consts.TestUserDiscordId);
            }

            try
            {
                await ReplyAsync(string.Format("Test Run Has Started For: {0}", uplandUsername.ToUpper()));

                CollectionOptimizer optimizer = new CollectionOptimizer();
                OptimizerRunRequest runRequest = new OptimizerRunRequest(uplandUsername.ToLower(), qualityLevel);
                await optimizer.RunAutoOptimization(new RegisteredUser
                {
                    DiscordUserId = Consts.TestUserDiscordId,
                    DiscordUsername = "TEST_USER_NAME",
                    UplandUsername = uplandUsername.ToLower()
                },
                runRequest);

                return;
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminOptimizerRun", ex.Message);
                await ReplyAsync(string.Format("Test Run Has Failed For: {0} - {1}", uplandUsername.ToUpper(), ex.Message));
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

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(Consts.TestUserDiscordId);

            if (currentRun.Status == Consts.RunStatusCompleted)
            {
                byte[] resultBytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(currentRun.Results));
                using (Stream stream = new MemoryStream())
                {
                    stream.Write(resultBytes, 0, resultBytes.Length);
                    stream.Seek(0, SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(stream, "AdminOptimizerResults.txt");
                }
            }
        }

        [Command("AdminPopulateCollections")]
        public async Task AdminPopulateCollections()
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await ReplyAsync(string.Format("Running Collection Update in Child Task"));
                await _localDataManager.PopulateDatabaseCollectionInfo();
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminPopulateCollections", ex.Message);
                await ReplyAsync(string.Format("Update Failed: {0}", ex.Message));
            }
        }

        [Command("ToggleBlockchainUpdates")]
        public async Task ToggleBlockchainUpdates()
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                bool enableUpdates = !bool.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES));
                _localDataManager.UpsertConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES, enableUpdates.ToString());
                await ReplyAsync(string.Format("Blockchain Updating is now {0}.", enableUpdates ? "Enabled" : "Disabled"));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - ToggleBlockchainUpdates", ex.Message);
                await ReplyAsync(string.Format("Failed To Toggle BlockchainUpdates: {0}", ex.Message));
            }
        }

        [Command("AdminPropsUnderConstruction")]
        public async Task AdminPropsUnderConstruction(int userlevel)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }
            await ReplyAsync(string.Format("Running Under Construction Report. This may take up to two hours."));

            List<string> stakingReport = await _informationProcessor.GetBuildingsUnderConstruction(userlevel);
            await ReplyAsync(string.Format("Construction Report Completed!"));

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, stakingReport));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("StakingReport_UserLevel_{0}.csv", Upland.InformationProcessor.HelperFunctions.TranslateUserLevel(userlevel)));
            }
        }

        [Command("SetUserPaid")]
        public async Task SetUserPaid(string uplandUsername)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                _localDataManager.SetRegisteredUserPaid(uplandUsername);
                await ReplyAsync(string.Format("{0} is now a Supporter.", uplandUsername));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - SetUserPaid", ex.Message);
                await ReplyAsync(string.Format("Failed: {0}", ex.Message));
            }
        }

        [Command("AdminResyncPropertyList")]
        public async Task AdminResyncPropertyList(string action, string propertyList)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await ReplyAsync(string.Format("Resyncing Properties."));
                await _informationProcessor.ResyncPropsList(action, propertyList);
                await ReplyAsync(string.Format("Properties Resynced."));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminResyncPropertyList", ex.Message);
                await ReplyAsync(string.Format("Failed Resyncing Properties: {0}", ex.Message));
            }
        }

        [Command("AdminRebuildPropertyStructures")]
        public async Task AdminRebuildPropertyStructures()
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await _informationProcessor.RebuildPropertyStructures();
                await ReplyAsync(string.Format("PropertyStructures Rebuilt."));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminRebuildPropertyStructures", ex.Message);
                await ReplyAsync(string.Format("Failed Rebuilding PropertyStructures: {0}", ex.Message));
            }
        }

        [Command("AdminRefreshCityById")]
        public async Task AdminRefreshCityById(string type, int cityId)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await ReplyAsync(string.Format("Performing {0} of CityId {1}", type.ToUpper(), cityId));
                await _informationProcessor.RefreshCityById(type.ToUpper(), cityId);
                await ReplyAsync(string.Format("CityId {0} Refreshed.", cityId));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminRefreshCityById", ex.Message);
                await ReplyAsync(string.Format("Failed Refreshing CityId {0}: {1}", cityId, ex.Message));
            }
        }
    }
}
