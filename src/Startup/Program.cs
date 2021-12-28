using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Upland.CollectionOptimizer;
using Upland.InformationProcessor;
using Upland.Infrastructure.Blockchain;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;

class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;

    private Timer _refreshTimer;
    private Timer _blockchainUpdateTimer;
    private Timer _sendTimer;

    private LocalDataManager _localDataManager;
    private UplandApiManager _uplandApiManager;
    private BlockchainManager _blockchainManager;

    private BlockchainPropertySurfer _blockchainPropertySurfer;
    private BlockchainSendFinder _blockchainSendFinder;
    private ForSaleProcessor _forSaleProcessor;
    private InformationProcessor _informationProcessor;
    private ProfileAppraiser _profileAppraiser;
    private ResyncProcessor _resyncProcessor;

    /*
    static async Task Main(string[] args) // DEBUG FUNCTION
    {
        CollectionOptimizer collectionOptimizer = new CollectionOptimizer();

        LocalDataManager localDataManager = new LocalDataManager();
        UplandApiManager uplandApiManager = new UplandApiManager();
        BlockchainManager blockchainManager = new BlockchainManager();

        BlockchainPropertySurfer blockchainPropertySurfer = new BlockchainPropertySurfer(localDataManager, uplandApiManager, blockchainManager);
        BlockchainSendFinder blockchainSendFinder = new BlockchainSendFinder(localDataManager, blockchainManager);
        ForSaleProcessor forSaleProcessor = new ForSaleProcessor(localDataManager);
        InformationProcessor informationProcessor = new InformationProcessor(localDataManager, uplandApiManager, blockchainManager);
        ProfileAppraiser profileAppraiser = new ProfileAppraiser(localDataManager, uplandApiManager);
        ResyncProcessor resyncProcessor = new ResyncProcessor(localDataManager, uplandApiManager);

        string username;
        string qualityLevel;
        List<string> output = new List<string>();

        // Populate City
        //List<double> cityCoordinates = Upland.InformationProcessor.HelperFunctions.GetCityAreaCoordinates(16);
        //await localDataManager.PopulateAllPropertiesInArea(cityCoordinates[0], cityCoordinates[1], cityCoordinates[2], cityCoordinates[3], 16, false);
        //localDataManager.DetermineNeighborhoodIdsForCity(16);

        //new Program().InitializeRefreshTimer();

        /// Test Optimizer
        //OptimizerRunRequest runRequest = new OptimizerRunRequest("cyclonix123", 7, true);
        //await collectionOptimizer.RunAutoOptimization(new RegisteredUser(), runRequest);

        // Populate initial City Data
        //await localDataManager.PopulateNeighborhoods();
        //await localDataManager.PopulateDatabaseCollectionInfo();
        //await localDataManager.PopulateStreets();

        // Test Information Processing Functions
        //output = await informationProcessor.GetCollectionPropertiesForSale(177, "PRICE", "ALL", "TXT");
        //output = await informationProcessor.GetCityPropertiesForSale(1, "Price", "All", "TXT");
        //output = await informationProcessor.GetNeighborhoodPropertiesForSale(235, "Price", "All");
        // output = await informationProcessor.GetBuildingPropertiesForSale("City", 0, "markup", "all", "CSV");
        //output = informationProcessor.GetCityInformation("TXT"); 
        //output = informationProcessor.GetAllProperties("Street", 31898, "CSV");
        //output = await informationProcessor.GetStreetPropertiesForSale(28029, "MARKUP", "ALL", "CSV");
        //output = await informationProcessor.GetAssetsByTypeAndUserName("NFLPA", "stoney300", "txt");
        //output = await informationProcessor.GetPropertyInfo("loyldoyl", "TXT");
        //output = await informationProcessor.GetBuildingsUnderConstruction(1);
        //List<SaleHistoryQueryEntry> entries = localDataManager.GetSaleHistoryByPropertyId(79565478804919);
        //output = informationProcessor.GetSaleHistoryByType("Property", "10, 9843 S Exchange Ave", "txt");
        //output = informationProcessor.SearchProperties(10, "3101 W", "TXT");
        //output = forSaleProcessor.GetBuildingPropertiesForSale("city", 0, "Price", "all", "txt");
        //output = informationProcessor.GetAllProperties("NEIGHBORHOOD", 1300, "TXT");
        //await File.WriteAllTextAsync(@"C:\Users\chri1\Desktop\Upland\OptimizerBot\test.txt", string.Join(Environment.NewLine, output));

        // Test Repo Actions
        //List<Decoration> nflpaLegits = await uplandApiManager.GetDecorationsByUsername("atomicpop");

        // Populate CityProps And Neighborhoods
        //await localDataManager.PopulateAllPropertiesInArea(40.921864, 40.782411, -73.763343, -73.942215, 29, true);
        //foreach (int cityId in Consts.Cities.Keys) { localDataManager.DetermineNeighborhoodIdsForCity(cityId); }
        //await localDataManager.PopulateCollectionPropertiesByCityId(16);
        //localDataManager.DetermineNeighborhoodIdsForCity(16);

        // Rebuild Property Structure List
        //await informationProcessor.RebuildPropertyStructures();
        //await informationProcessor.RunCityStatusUpdate();
        //await informationProcessor.RefreshCityById("PART", 1);
        //List<HistoryAction> items = await blockchainManager.GetPropertyActionsFromTime(DateTime.Now.AddMinutes(-50), 15);

        // Dictionary<string, double> stakes = await blockchainManager.GetStakedSpark();

        // List<KeyValuePair<string, double>> list = stakes.ToList().OrderByDescending(s => s.Value).ToList();
        //localDataManager.UpsertConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES, true.ToString());

        //await blockchainPropertySurfer.RunBlockChainUpdate(); // .BuildBlockChainFromDate(startDate);
        //await blockchainPropertySurfer.BuildBlockChainFromBegining();
        //await resyncProcessor.ResyncPropsList("SetMonthlyEarnings", "81369886458957,81369920013374,81369651577913,81369467028575,81369500582974");
        //await resyncProcessor.ResyncPropsList("CityUnmintedFullResync", "1");
        //await blockchainSendFinder.RunBlockChainUpdate();
    }
    */
    
    static void Main(string[] args) 
        => new Program().RunBotAsync().GetAwaiter().GetResult();
    

    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient();
        _commands = new CommandService();

        _localDataManager = new LocalDataManager();
        _uplandApiManager = new UplandApiManager();
        _blockchainManager = new BlockchainManager();

        _blockchainPropertySurfer = new BlockchainPropertySurfer(_localDataManager, _uplandApiManager, _blockchainManager);
        _blockchainSendFinder = new BlockchainSendFinder(_localDataManager, _blockchainManager);
        _forSaleProcessor = new ForSaleProcessor(_localDataManager);
        _informationProcessor = new InformationProcessor(_localDataManager, _uplandApiManager, _blockchainManager);
        _profileAppraiser = new ProfileAppraiser(_localDataManager, _uplandApiManager);
        _resyncProcessor = new ResyncProcessor(_localDataManager, _uplandApiManager);

        _services = new ServiceCollection()
            .AddSingleton(_localDataManager)
            .AddSingleton(_forSaleProcessor)
            .AddSingleton(_informationProcessor)
            .AddSingleton(_profileAppraiser)
            .AddSingleton(_resyncProcessor)
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .BuildServiceProvider();

        string token = System.IO.File.ReadAllText(@"auth.txt");

        _client.Log += clientLog;

        InitializeRefreshTimer();
        InitializeBlockchainUpdateTimer();
        InitializeSendTimer();

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
                    switch (result.ErrorReason)
                    {
                        case "Unknown command.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: I don't know that command. Try running !Help."));
                            break;
                        case "The input text has too many parameters.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: There are too many parameters on that command."));

                            await context.Channel.SendMessageAsync(string.Format("{0}", string.Join(Environment.NewLine,
                                Startup.HelperFunctions.GetHelpTextForCommandNumber(
                                    Startup.HelperFunctions.GetHelpNumber(context.Message.Content.Split("!")[1].Split(" ")[0].ToUpper())
                                ))));

                            break;
                        case "The input text has too few parameters.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: There are too few parameters on that command."));

                            await context.Channel.SendMessageAsync(string.Format("{0}", string.Join(Environment.NewLine,
                                Startup.HelperFunctions.GetHelpTextForCommandNumber(
                                    Startup.HelperFunctions.GetHelpNumber(context.Message.Content.Split("!")[1].Split(" ")[0].ToUpper())
                                ))));
                            break;
                        case "Failed to parse Int32.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: Looks like you typed a word instead of a number."));

                            await context.Channel.SendMessageAsync(string.Format("{0}", string.Join(Environment.NewLine,
                                Startup.HelperFunctions.GetHelpTextForCommandNumber(
                                    Startup.HelperFunctions.GetHelpNumber(context.Message.Content.Split("!")[1].Split(" ")[0].ToUpper())
                                ))));
                            break;
                        case "Object reference not set to an instance of an object.":
                        case "The server responded with error 503: ServiceUnavailable":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: Upland may be in Maintenance right now, try again later. If not something has gone horribly wrong, please contact Grombrindal."));
                            _localDataManager.CreateErrorLog("Program.cs - HandleCommandAsync", result.ErrorReason);
                            break;
                        case "The server responded with error 40005: Request entity too large":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: That file exceeds the size limit set discord unfortunaley. Try requesting it as a CSV instead."));
                            break;
                        default:
                            await context.Channel.SendMessageAsync(string.Format("ERROR: Contact Grombrindal."));
                            _localDataManager.CreateErrorLog("Program.cs - HandleCommandAsync", result.ErrorReason);
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

    private void InitializeBlockchainUpdateTimer()
    {
        _blockchainUpdateTimer = new Timer();
        _blockchainUpdateTimer.Elapsed += (sender, e) =>
        {
            Task child = Task.Factory.StartNew(async () =>
            {
                await _blockchainPropertySurfer.RunBlockChainUpdate();
            });
        };
        _blockchainUpdateTimer.Interval = 30000; // Every 30 Seconds
        _blockchainUpdateTimer.Start();
    }

    private void InitializeSendTimer()
    {
        _sendTimer = new Timer();
        _sendTimer.Elapsed += (sender, e) =>
        {
            Task child = Task.Factory.StartNew(async () =>
            {
                await _blockchainSendFinder.RunBlockChainUpdate();
            });
        };
        _sendTimer.Interval = 300000; // Every 5 Minutes
        _sendTimer.Start();
    }

    private async Task RunRefreshActions()
    {
        DateTime time = DateTime.UtcNow;
        if (time.Hour == 6)
        {
            // Rebuild the structures list every night
            try
            {
                Console.WriteLine(string.Format("{0}: Rebuilding Structures", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
                await _informationProcessor.RebuildPropertyStructures();
                Console.WriteLine(string.Format("{0}: Rebuilding Complete", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now)));
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("Program.cs - RunRefreshActions - Rebuild Structures", ex.Message);
                Console.WriteLine(string.Format("{0}: Rebuilding Structures Failed: {1}", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), ex.Message));
            }
        }
    }
}