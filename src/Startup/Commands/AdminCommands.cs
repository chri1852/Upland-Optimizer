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

        public AdminCommands(InformationProcessor informationProcessor)
        {
            _random = new Random();
            _informationProcessor = informationProcessor;
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
                await ReplyAsync(string.Format("Test Run Has Started For: {0}", uplandUsername.ToUpper()));

                CollectionOptimizer optimizer = new CollectionOptimizer();
                await optimizer.RunAutoOptimization(new RegisteredUser
                {
                    DiscordUserId = Consts.TestUserDiscordId,
                    DiscordUsername = "TEST_USER_NAME",
                    UplandUsername = uplandUsername.ToLower()
                },
                qualityLevel);

                return;
            }
            catch (Exception ex)
            {
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

            LocalDataManager localDataManager = new LocalDataManager();
            OptimizationRun currentRun = localDataManager.GetLatestOptimizationRun(Consts.TestUserDiscordId);

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

            LocalDataManager localDataManager = new LocalDataManager();

            try
            {
                await ReplyAsync(string.Format("Running Collection Update in Child Task"));
                await localDataManager.PopulateDatabaseCollectionInfo();
            }
            catch (Exception ex)
            {
                await ReplyAsync(string.Format("Update Failed: {0}", ex.Message));
            }
        }

        [Command("AdminPropertyInfo")]
        public async Task AdminPropertyInfo(string userName, string fileType = "TXT")
        {
            LocalDataManager localDataManager = new LocalDataManager();

            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            List<string> propertyData = await _informationProcessor.GetPropertyInfo(userName.ToLower(), fileType.ToUpper());

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, propertyData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("PropertyInfo_{0}.{1}", userName.ToUpper(), fileType.ToUpper() == "TXT" ? "txt" : "csv"));
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
                LocalDataManager localDataManager = new LocalDataManager();
                bool enableUpdates = !bool.Parse(localDataManager.GetConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES));
                localDataManager.UpsertConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES, enableUpdates.ToString());
                await ReplyAsync(string.Format("Blockchain Updating is now {0}.", enableUpdates ? "Enabled" : "Disabled"));
            }
            catch (Exception ex)
            {
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

            LocalDataManager localDataManager = new LocalDataManager();

            try
            {
                localDataManager.SetRegisteredUserPaid(uplandUsername);
                await ReplyAsync(string.Format("{0} is now a Supporter.", uplandUsername));
            }
            catch (Exception ex)
            {
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
                await ReplyAsync(string.Format("Failed Refreshing CityId {0}: {1}", cityId, ex.Message));
            }
        }
    }
}
