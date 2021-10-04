using System;

namespace Startup
{
    class Program
    {
        static void Main(string[] args)
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
    }
}
