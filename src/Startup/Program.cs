using System;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.DiscordBot;
using Upland.Infrastructure.LocalData;

namespace Startup
{
    class Program
    {
        static async Task Main(string[] args)
        {

            //LocalDataManager dataManager = new LocalDataManager();
            //dataManager.CreateOptimizationRun(new OptimizationRun{DiscordUserId = 313795907755704321, Filename = "test_name.txt"});
            //dataManager.SetOptimizationRunStatus(new OptimizationRun { Id = 1, Status = "Completed" });
            //OptimizationRun test = dataManager.GetLatestOptimizationRun(313795907755704321);
            //return;

            // If there are args assume we have an automated request
            if (args != null && args.Length > 0)
            {
                await DiscordBotCommands.RunAutomatedRequest(args);
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
    }
}
