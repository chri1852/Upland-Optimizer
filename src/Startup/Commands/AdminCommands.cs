﻿using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Interfaces.Repositories;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Types;
using Upland.Interfaces.Managers;
using System.Text.Json;

namespace Startup.Commands
{
    public class AdminCommands : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _configuration;
        private readonly Random _random;
        private readonly IInformationProcessor _informationProcessor;
        private readonly IResyncProcessor _resyncProcessor;
        private readonly ILocalDataManager _localDataManager;
        private readonly IProfileAppraiser _profileAppraiser;
        private readonly IUplandApiRepository _uplandApiRepository;

        public AdminCommands(IInformationProcessor informationProcessor, IResyncProcessor resyncProcessor, ILocalDataManager localDataManager, IProfileAppraiser profileAppraiser, IConfiguration configuration, IUplandApiRepository uplandApiRepository)
        {
            _configuration = configuration;
            _random = new Random();
            _informationProcessor = informationProcessor;
            _resyncProcessor = resyncProcessor;
            _localDataManager = localDataManager;
            _profileAppraiser = profileAppraiser;
            _uplandApiRepository = uplandApiRepository;
        }

        private async Task<bool> checkIfAdmin(ulong discordUserId)
        {
            ulong adminId = ulong.Parse(this._configuration["AppSettings:AdminDiscordId"]);

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

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(Consts.TestUserId);
            if (currentRun != null && currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("Test User Run is in Progress, Please Wait."));
                return;
            }

            if (currentRun != null)
            {
                _localDataManager.DeleteOptimizerRuns(Consts.TestUserId);
            }

            try
            {
                await ReplyAsync(string.Format("Test Run Has Started For: {0}", uplandUsername.ToUpper()));

                CollectionOptimizer optimizer = new CollectionOptimizer(_localDataManager, _uplandApiRepository);
                OptimizerRunRequest runRequest = new OptimizerRunRequest(uplandUsername.ToLower(), qualityLevel);
                RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(1);
                registeredUser.UplandUsername = uplandUsername.ToLower();
                runRequest.AdminRun = true;
                await optimizer.RunAutoOptimization(registeredUser, runRequest);

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

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(Consts.TestUserId);

            if (currentRun.Status == Consts.RunStatusCompleted)
            {
                OptimizerResults optimizerResults = JsonSerializer.Deserialize<OptimizerResults>(Encoding.UTF8.GetString(currentRun.Results));
                byte[] resultBytes = Encoding.UTF8.GetBytes(CollectionOptimizer.BuildTextOutput(optimizerResults));
                using (Stream stream = new MemoryStream())
                {
                    stream.Write(resultBytes, 0, resultBytes.Length);
                    stream.Seek(0, SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(stream, "AdminOptimizerResults.txt");
                }
            }
        }

        [Command("AdminAppraisal")]
        public async Task AdminAppraisal(string uplandUsername)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            AppraisalResults appraiserOutput = new AppraisalResults();

            try
            {
                appraiserOutput = await _profileAppraiser.RunAppraisal(new RegisteredUser
                {
                    Id = Consts.TestUserId,
                    DiscordUsername = "TEST_USER_NAME",
                    UplandUsername = uplandUsername.ToLower()
                });
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("Commands - Appraisal", ex.Message);
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            string appraisalString = string.Join(Environment.NewLine, _profileAppraiser.BuildAppraisalTxtStrings(appraiserOutput));

            byte[] resultBytes = Encoding.UTF8.GetBytes(appraisalString);
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("{0}_Appraisal.txt", uplandUsername));
            }
        }

        [Command("AdminPopulateCollections")]
        public async Task AdminPopulateCollections(int cityId)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await ReplyAsync(string.Format("Running Collection Update in Child Task"));
                await _localDataManager.PopulateDatabaseCollectionInfo(cityId);
                await ReplyAsync(string.Format("Collection Update Complete"));
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

        [Command("GrantRuns")]
        public async Task GrantRuns(string uplandUsername, int runCount)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);
                registeredUser.RunCount -= runCount;
                _localDataManager.UpdateRegisteredUser(registeredUser);
                await ReplyAsync(string.Format("Granted {0} {1} runs.", uplandUsername, runCount));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - GrantRuns", ex.Message);
                await ReplyAsync(string.Format("Failed: {0}", ex.Message));
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
                RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUsername);
                registeredUser.Paid = true;
                _localDataManager.UpdateRegisteredUser(registeredUser);
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
                await _resyncProcessor.ResyncPropsList(action, propertyList);
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
                _informationProcessor.RebuildPropertyStructures();
                await ReplyAsync(string.Format("PropertyStructures Rebuilt."));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminRebuildPropertyStructures", ex.Message);
                await ReplyAsync(string.Format("Failed Rebuilding PropertyStructures: {0}", ex.Message));
            }
        }

        [Command("AdminLoadCityAndProperties")]
        public async Task AdminLoadCityAndProperties(int cityId)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await ReplyAsync(string.Format("Performing Load of CityId {0}",cityId));
                await _informationProcessor.LoadMissingCityProperties(cityId);
                await ReplyAsync(string.Format("CityId {0} Loaded.", cityId));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminLoadCityAndProperties", ex.Message);
                await ReplyAsync(string.Format("Failed Loading CityId {0}: {1}", cityId, ex.Message));
            }
        }

        [Command("AdminLoadStreets")]
        public async Task AdminLoadStreets()
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await ReplyAsync(string.Format("Loading Streets..."));
                await _localDataManager.PopulateStreets();
                await ReplyAsync(string.Format("Loading Streets Complete."));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminLoadStreets", ex.Message);
                await ReplyAsync(string.Format("Failed Loading Streets: {0}", ex.Message));
            }
        }

        [Command("AdminResetLockedProps")]
        public async Task AdminResetLockedProps(int cityId)
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await ReplyAsync(string.Format("Performing Relock of CityId {0}", cityId));
                await _informationProcessor.ResetLockedPropsToLocked(cityId);
                await ReplyAsync(string.Format("CityId {0} Relocked.", cityId));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("AdminCommands - AdminResetLockedProps", ex.Message);
                await ReplyAsync(string.Format("Failed Relocking CityId {0}: {1}", cityId, ex.Message));
            }
        }
    }
}
