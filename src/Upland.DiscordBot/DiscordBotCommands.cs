using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.DiscordBot
{
    public static class DiscordBotCommands
    {
        public static async Task<string> RunAutomatedRequest(string[] args)
        {
            switch (args[0])
            {
                // Run Optimization
                case "1":
                    //CollectionOptimizer collectionOptimizer = new CollectionOptimizer();
                   // await collectionOptimizer.RunAutoOptimization(args[1], int.Parse(args[2]));
                    break;

                // Get Optimization Status
                case "2":
                    break;

                // Return Optimization File
                case "3":
                    break;

                // Register User
                case "4":
                    return await RegisterUser(args);
            }

            return "Fail";
        }

        // args[0] = Action Type
        // args[1] = UplandUsername
        // args[2] = DiscordUsername
        // args[3] = DiscordUserId
        private static async Task<string> RegisterUser(string[] args)
        {
            LocalDataManager localDataManager = new LocalDataManager();

            try
            {
                RegisteredUser registeredUser = localDataManager.GetRegisteredUser(args[1]);
                return registeredUser.DiscordUsername;
            }
            catch
            {
                return "You Don't Exist Buddy";
            }
        }
    }
}
