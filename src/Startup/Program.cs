using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using Upland.Types.UplandApiTypes;

namespace Startup
{
    class Program
    {
        static async Task Main(string[] args)
        {
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
