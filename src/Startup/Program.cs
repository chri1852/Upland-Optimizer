using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Upland.InformationProcessor;
using System.Timers;
using System.Text.Json;

// ONLY UNCOMMENT FOR DEBUGING
using Upland.CollectionOptimizer;  
using Upland.Infrastructure.LocalData;
using System.Collections.Generic;
using System.IO;
using Upland.Types.Types;
using System.Linq;
using Upland.Infrastructure.Blockchain;
using Upland.Types.BlockchainTypes;
using Upland.Types;
using Upland.Infrastructure.UplandApi;
using Upland.Types.UplandApiTypes;

class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;
    private InformationProcessor _informationProcessor;
    private Timer _refreshTimer;

    
    static async Task Main(string[] args) // DEBUG FUNCTION
    {
        LocalDataManager localDataManager = new LocalDataManager();
        CollectionOptimizer collectionOptimizer = new CollectionOptimizer();
        InformationProcessor informationProcessor = new InformationProcessor();
        BlockchainRepository blockchainRepository = new BlockchainRepository();
        UplandApiManager uplandApiManager = new UplandApiManager();
        BlockchainManager blockchainManager = new BlockchainManager();
        BlockchainPropertySurfer blockchainPropertySurfer = new BlockchainPropertySurfer();

        string username;
        string qualityLevel;
        List<string> output = new List<string>();

        // Populate City
        //List<double> cityCoordinates = Upland.InformationProcessor.HelperFunctions.GetCityAreaCoordinates(16);
        //await localDataManager.PopulateAllPropertiesInArea(cityCoordinates[0], cityCoordinates[1], cityCoordinates[2], cityCoordinates[3], 16, false);
        //localDataManager.DetermineNeighborhoodIdsForCity(16);

        //new Program().InitializeRefreshTimer();

        /// Test Optimizer
        //Console.Write("Enter the Upland Username: ");
        //username = Console.ReadLine();
        //Console.Write("Enter the Level (1-8)....: ");
        //qualityLevel = Console.ReadLine();
        //await collectionOptimizer.RunDebugOptimization(username, int.Parse(qualityLevel), 201, 20, 1000);
        //await collectionOptimizer.RunDebugOptimization(username, int.Parse(qualityLevel));

        // Populate initial City Data
        //await localDataManager.PopulateNeighborhoods();
        //await localDataManager.PopulateDatabaseCollectionInfo();
        //await localDataManager.PopulateStreets();

        // Test Information Processing Functions
        //output = await informationProcessor.GetCollectionPropertiesForSale(177, "PRICE", "ALL", "TXT");
        //output = await informationProcessor.GetSalesDataByCityId(1);
        //output = await informationProcessor.GetNeighborhoodPropertiesForSale(235, "Price", "All");
        // output = await informationProcessor.GetBuildingPropertiesForSale("City", 0, "markup", "all", "CSV");
        //output = informationProcessor.GetCityInformation("TXT"); 
        //output = informationProcessor.GetAllProperties("Street", 31898, "CSV");
        //output = await informationProcessor.GetStreetPropertiesForSale(28029, "MARKUP", "ALL", "CSV");
        //output = await informationProcessor.GetAssetsByTypeAndUserName("nflpa", "loyldoyl", "txt");
        //output = await informationProcessor.GetPropertyInfo("loyldoyl", "TXT");
        //await File.WriteAllTextAsync(@"C:\Users\chri1\Desktop\Upland\OptimizerBot\test_file.txt", string.Join(Environment.NewLine, output));

        // Test Repo Actions
        //List<Decoration> nflpaLegits = await uplandApiManager.GetDecorationsByUsername("atomicpop");

        // Populate CityProps And Neighborhoods
        //await localDataManager.PopulateAllPropertiesInArea(40.656588, 40.492300, -74.031335, -74.264108, 16, true);
        //foreach (int cityId in Consts.Cities.Keys) { localDataManager.DetermineNeighborhoodIdsForCity(cityId); }
        //await localDataManager.PopulateCollectionPropertiesByCityId(16);
        //localDataManager.DetermineNeighborhoodIdsForCity(16);

        // Rebuild Property Structure List
        //await informationProcessor.RebuildPropertyStructures();
        //await informationProcessor.RunCityStatusUpdate(true);

        //List<HistoryAction> items = await blockchainManager.GetPropertyActionsFromTime(DateTime.Now.AddMinutes(-50), 15);

        // Dictionary<string, double> stakes = await blockchainManager.GetStakedSpark();

        // List<KeyValuePair<string, double>> list = stakes.ToList().OrderByDescending(s => s.Value).ToList();

        await blockchainPropertySurfer.BuildBlockChainFromBegining();
    }
    
    
    /*
    static void Main(string[] args) 
        => new Program().RunBotAsync().GetAwaiter().GetResult();
    */

    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient();
        _commands = new CommandService();
        _informationProcessor = new InformationProcessor();

        _services = new ServiceCollection()
            .AddSingleton(_informationProcessor)
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .BuildServiceProvider();

        string token = System.IO.File.ReadAllText(@"auth.txt");

        _client.Log += clientLog;

        InitializeRefreshTimer();

        await RegisterCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, JsonSerializer.Deserialize<Dictionary<string, string>>(System.IO.File.ReadAllText(@"appsettings.json"))["DiscordBotToken"]);

        await _client.StartAsync();

        await Task.Delay(-1);
    }

    private Task clientLog(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }

    public async Task RegisterCommandsAsync()
    {
        _client.MessageReceived += HandleCommandAsync;
        await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }

    private async Task HandleCommandAsync(SocketMessage arg)
    {
        SocketUserMessage message = arg as SocketUserMessage;

        if (message == null)
        {
            Task child = Task.Factory.StartNew(async () =>
            {
                Console.WriteLine(string.Format("{0}: New User Joined!", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
            });
            return;
        }

        SocketCommandContext context = new SocketCommandContext(_client, message);

        if (message.Author.IsBot)
        {
            return;
        }

        int argPos = 0;
        if (message.HasStringPrefix("!", ref argPos))
        {
            if (context.Channel.Name == "general" || context.Channel.Name == "tech-issues")
            {
                Task wrongChannelChild = Task.Factory.StartNew(async () =>
                {
                    await context.Channel.SendMessageAsync(string.Format("Spam the bot in bot-spam my dude."));
                });
                return;
            }

            if (context.Channel.Name == "help" && !message.Content.ToUpper().Contains("HELP"))
            {
                Task helpChild = Task.Factory.StartNew(async () =>
                {
                    await context.Channel.SendMessageAsync(string.Format("Spam the bot in bot-spam my dude."));
                });
                return;
            }

            Task child = Task.Factory.StartNew(async () =>
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(string.Format("{0}: {1}", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), result.ErrorReason));
                    Console.WriteLine(string.Format("{0}: {1} - {2}", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), message.Author.Username, message.Content));
                    switch(result.ErrorReason)
                    {
                        case "Unknown command.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: I don't know that command. Try running !Help."));
                            break;
                        case "The input text has too many parameters.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: There are too many parameters on that command. Try looking at the !Help documentation for that command."));
                            break;
                        case "The input text has too few parameters.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: There are too few parameters on that command. Try looking at the !Help documentation for that command."));
                            break;
                        case "Failed to parse Int32.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: Looks like you typed a word instead of a number. Try looking at the !Help documentation for that command."));
                            break;
                        case "Object reference not set to an instance of an object.":
                        case "The server responded with error 503: ServiceUnavailable":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: Upland may be in Maintenance right now, try again later. If not something has gone horribly wrong, please contact Grombrindal."));
                            break;
                        case "The server responded with error 40005: Request entity too large":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: That file exceeds the size limit set discord unfortunaley. Try requesting it as a CSV instead."));
                            break;
                        default:
                            await context.Channel.SendMessageAsync(string.Format("ERROR: Contact Grombrindal."));
                            break;
                    }
                }
                else
                {
                    Console.WriteLine(string.Format("{0}: {1} - {2}", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), message.Author.Username, message.Content));
                }
            });
        }
    }

    private void InitializeRefreshTimer()
    {
        _refreshTimer = new Timer();
        _refreshTimer.Elapsed += (sender, e) =>
        {
            Task child = Task.Factory.StartNew(async () =>
            {
                await RunRefreshActions();
            });
        };
        _refreshTimer.Interval = 3600000;
        _refreshTimer.Start();
    }

    private async Task RunRefreshActions()
    {
        DateTime time = DateTime.Now;
        if (time.Hour == 0) 
        {
            InformationProcessor informationProcessor = new InformationProcessor();

            // Rebuild the structures list every night
            try
            {
                Console.WriteLine(string.Format("{0}: Rebuilding Structures", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
                await informationProcessor.RebuildPropertyStructures();
                Console.WriteLine(string.Format("{0}: Rebuilding Complete", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0}: Rebuilding Structures Failed: {1}", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), ex.Message));
            }

            // Only Rebuild All on Sundays
            if (time.DayOfWeek == DayOfWeek.Sunday)
            {
                try
                {
                    Console.WriteLine(string.Format("{0}: Refreshing All Cities", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
                    await informationProcessor.RunCityStatusUpdate(true);
                    Console.WriteLine(string.Format("{0}: Refreshing All Cities Complete", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("{0}: Refreshing All Cities Failed: {1}", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), ex.Message));
                }
            }
            else
            {
                try
                {
                    Console.WriteLine(string.Format("{0}: Refreshing Non Sold Out Cities", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
                    await informationProcessor.RunCityStatusUpdate(false);
                    Console.WriteLine(string.Format("{0}: Refreshing Non Sold Out Cities Complete", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("{0}: Refreshing Non Sold Out Cities Failed: {1}", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), ex.Message));
                }
            }
        }
    }
}