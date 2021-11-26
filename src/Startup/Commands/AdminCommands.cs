﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            if(discordUserId != Consts.AdminDiscordId)
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

        [Command("AdminRebuildPropertyStatus")]
        public async Task AdminRebuildPropertyStatus()
        {
            if (!await checkIfAdmin(Context.User.Id))
            {
                return;
            }

            try
            {
                await _informationProcessor.RunCityStatusUpdate(true);
                await ReplyAsync(string.Format("Property Status Rebuilt."));
            }
            catch (Exception ex)
            {
                await ReplyAsync(string.Format("Failed Rebuilding Property Status: {0}", ex.Message));
            }
        }
    }
}
