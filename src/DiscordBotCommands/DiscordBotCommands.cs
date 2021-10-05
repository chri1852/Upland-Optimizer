using Upland.CollectionOptimizer;
using Upland.Infrastructure.LocalData;
using System.Threading.Tasks;

namespace Upland.DiscordBotCommands
{
    public static class DiscordBotCommands
    {
        private static async Task RunAutomatedRequest(string[] args)
        {
            switch (args[0])
            {
                // Run Optimization
                case "1":
                    CollectionOptimizer test;
                    CollectionOptimizer collectionOptimizer = new CollectionOptimizer();
                    await collectionOptimizer.RunAutoOptimization(args[1], int.Parse(args[2]));
                    break;

                // Get Optimization Status
                case "2":
                    break;

                // Return Optimization File
                case "3":
                    break;
            }
        }
    }
}
