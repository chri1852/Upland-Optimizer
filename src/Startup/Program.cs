using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Startup
{
    class Program
    {
        static async Task Main(string[] args)
        {

            LocalDataManager dataManager = new LocalDataManager();
            dataManager.CreateOptimizationRun(new OptimizationRun{DiscordUserId = 313795907755704321, Filename = "test_name.txt"});
            //dataManager.SetOptimizationRunStatus(new OptimizationRun { Id = 1, Status = "Completed" });
            OptimizationRun test = dataManager.GetLatestOptimizationRun(313795907755704321);
            return;

            // If there are args assume we have an automated request
            if (args != null && args.Length > 0)
            {
                await RunAutomatedRequest(args);
                return;
            }

            // Else this is a manual call to the program
            string continueProgram = "Y";
            CollectionOptimizer collectionOptimizer = new CollectionOptimizer();

            while (continueProgram.ToUpper() != "N" && continueProgram.ToUpper() != "NO" && continueProgram.ToUpper() != "0")
            {
                Console.WriteLine("Select a Program to Run");
                Console.WriteLine("-----------------------");
                Console.WriteLine("1. Collection Optimizer");
                Console.WriteLine("2. Database Rebuild    ");
                Console.WriteLine();
                Console.Write    ("Choice...............: ");
                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await collectionOptimizer.OptimizerStartPoint();
                        continueProgram = ContinueLoop();
                        break;

                    case "2":

                        continueProgram = ContinueLoop();
                        break;

                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid Selection");
                        Console.WriteLine();
                        Console.ResetColor();
                        break;
                }
            }
        }

        private static string ContinueLoop()
        {
            Console.WriteLine();
            Console.Write("Continue? (Y/N)...: ");
            string continueProgram = Console.ReadLine();
            Console.WriteLine();

            return continueProgram.ToUpper();
        }

        private static async Task RunAutomatedRequest(string[] args)
        {
            switch (args[0])
            {
                // Run Optimization
                case "1":
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
