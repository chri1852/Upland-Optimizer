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

            while (continueProgram.ToUpper() != "N" && continueProgram.ToUpper() != "NO" && continueProgram.ToUpper() != "0")
            {
                Console.WriteLine("Select a Program to Run");
                Console.WriteLine("-----------------------");
                Console.WriteLine("1. Collection Optimizer");
                Console.WriteLine("2. Database Rebuild    ");
                Console.Write    ("Choice...............: ");
                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await tempFunction();
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
            Console.Write("Continue? (Y/N)...: ");
            string continueProgram = Console.ReadLine();
            Console.WriteLine();

            return continueProgram.ToUpper();
        }




        private static async Task tempFunction()
        {
            string username;
            string qualityLevel;
            string repeat;
            Dictionary<int, Collection> allCollections;
            Dictionary<int, string> allCities;

            UplandApiRepository uplandApiRepository = new UplandApiRepository();
            allCollections = PopulateAllCollections();
            allCities = await PopulateAllCities(uplandApiRepository);

            // Optimizer Quality goes from 1 to 8, you can run it higher but it will slow greatly expecially on larger profiles.
            // For reference here is the output for ben68, net worth 82,216,080.62, with 4,094 properties at various quality levels

            // |  Monthly UPX  | Quality |     Time     |
            // | 1,686,660.00  |    -    |       -      |
            // | 1,672,300.00  |    1    | 00:00:00.593 |
            // | 1,679,993.64  |    2    | 00:00:00.843 |
            // | 1,680,027.55  |    3    | 00:00:01.483 |
            // | 1,680,027.55  |    4    | 00:00:04.199 |
            // | 1,688,251.14  |    5    | 00:00:11.071 |
            // | 1,688,285.05  |    6    | 00:00:53.180 |
            // | 1,688,725.65  |    7    | 00:09:43.214 |
            // | 1,688,725.65  |    8    | 01:09:59.996 |

            Console.WriteLine("Collection Optimizer");
            Console.WriteLine();
            do
            {
                Console.Write("Enter the Upland Username: ");
                username = Console.ReadLine();
                Console.Write("Enter the Level (1-8)....: ");
                qualityLevel = Console.ReadLine();
                Console.WriteLine();

                CollectionOptimizer optimizer = new CollectionOptimizer(allCollections, allCities, true);

                //try
               // {
                    await optimizer.RunOptimization(username, int.Parse(qualityLevel));
               // }
               // catch
               // {
               //     Console.WriteLine();
              //      Console.ForegroundColor = ConsoleColor.Red;
              //      Console.WriteLine("An Error Occured. Bad Username Perhaps?");
              //      Console.ResetColor();
              //      Console.WriteLine();
           //     }
            //
                Console.Write("Run Script Again (Y/N)...: ");
                repeat = Console.ReadLine();
                Console.WriteLine();
            } while (repeat.ToUpper() != "N" && repeat.ToUpper() != "NO" && repeat.ToUpper() != "0");
        }

        private static Dictionary<int, Collection> PopulateAllCollections()
        {
            Dictionary<int, Collection> allCollections = new Dictionary<int, Collection>(); ;
            LocalDataManager dataManager = new LocalDataManager();
            List<Collection> collections = dataManager.GetCollections();
                
            foreach (Collection collection in collections)
            {
                allCollections.Add(collection.Id, collection);
            }

            return allCollections;
        }

        private static async Task<Dictionary<int, string>> PopulateAllCities(UplandApiRepository repository)
        {
            Dictionary<int, string> allCities = new Dictionary<int, string>();
            List<UplandCity> cities = await repository.GetCities();
            foreach (UplandCity city in cities)
            {
                allCities.Add(city.Id, city.Name);
            }

            return allCities;
        }
    }
}
