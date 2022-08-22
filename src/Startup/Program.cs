using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Startup.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using Upland.BlockchainSurfer;
using Upland.CollectionOptimizer;
using Upland.InformationProcessor;
using Upland.Infrastructure.Blockchain;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Interfaces.BlockchainSurfers;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Interfaces.Repositories;
using Upland.Types;
using Upland.Types.BlockchainTypes;
using Upland.Types.Enums;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;
using HelperFunctions = Upland.InformationProcessor.HelperFunctions;

class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;
    private IConfiguration _configuration;

    private Timer _refreshTimer;
    private Timer _blockchainUpdateTimer;

    
    private static async Task DebugFunction()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        LocalDataRepository localDataRepository = new LocalDataRepository(configuration);
        UplandApiRepository uplandApiRepository = new UplandApiRepository(configuration);
        BlockchainRepository blockchainRepository = new BlockchainRepository();

        LocalDataManager localDataManager = new LocalDataManager(uplandApiRepository, localDataRepository);
        UplandApiManager uplandApiManager = new UplandApiManager(uplandApiRepository);
        BlockchainManager blockchainManager = new BlockchainManager();

        CachingProcessor cachingProcessor = new CachingProcessor(localDataManager, uplandApiManager);
        PlayUplandMeSurfer playUplandMeSurfer = new PlayUplandMeSurfer(localDataManager, uplandApiManager, blockchainManager, configuration);
        USPKTokenAccSurfer uspkTokenAccSurfer = new USPKTokenAccSurfer(localDataManager, blockchainManager, configuration);
        UplandNFTActSurfer uplandNFTActSurfer = new UplandNFTActSurfer(localDataManager, uplandApiManager, blockchainManager, configuration);
        ForSaleProcessor forSaleProcessor = new ForSaleProcessor(localDataManager);
        InformationProcessor informationProcessor = new InformationProcessor(localDataManager, uplandApiManager, blockchainManager);
        //ProfileAppraiser profileAppraiser = new ProfileAppraiser(localDataManager, uplandApiManager, cachingProcessor);
        ResyncProcessor resyncProcessor = new ResyncProcessor(localDataManager, uplandApiManager, uplandNFTActSurfer);
        //MappingProcessor mappingProcessor = new MappingProcessor(localDataManager, profileAppraiser, cachingProcessor);
        WebProcessor webProcessor = new WebProcessor(localDataManager, uplandApiManager, cachingProcessor);
        CollectionOptimizer collectionOptimizer = new CollectionOptimizer(localDataManager);
        LeaderboardProcessor leaderboardProcessor = new LeaderboardProcessor(localDataManager, cachingProcessor);

        // Populate City
        //List<double> cityCoordinates = Upland.InformationProcessor.HelperFunctions.GetCityAreaCoordinates(16);
        //await localDataManager.PopulateAllPropertiesInArea(cityCoordinates[0], cityCoordinates[1], cityCoordinates[2], cityCoordinates[3], 16, false);
       // await localDataManager.PopulateNeighborhoods();
        await informationProcessor.LoadMissingCityProperties(37);
        //await localDataManager.PopulateDatabaseCollectionInfo(36);

        //await resyncProcessor.ResyncPropsList("EnclaveFix", "1");
        //await localDataManager.PopulateDatabaseCollectionInfo(10);

        /// Test Optimizer
        //OptimizerRunRequest runRequest = new OptimizerRunRequest("hornbrod", 7, true);
        //await collectionOptimizer.RunAutoOptimization(new RegisteredUser(), runRequest);

        // Test Appraiser
        //AppraisalResults results = await profileAppraiser.RunAppraisal(new RegisteredUser { Id = 1, UplandUsername = "hornbrod" });
        //await File.WriteAllTextAsync(@"C:\Users\chri1\Desktop\hornbrod.csv", string.Join(Environment.NewLine, profileAppraiser.BuildAppraisalCsvStrings(results)));

        // Populate initial City Data
        //await localDataManager.PopulateNeighborhoods();
        //await localDataManager.PopulateDatabaseCollectionInfo(36);
        //await localDataManager.PopulateStreets();
        //await informationProcessor.LoadMissingCityProperties(35);

        // Hunt Treasures
        //await informationProcessor.HuntTreasures(4, "oqtr232h2c23", TreasureTypeEnum.Standard);

        // Test Information Processing Functions
        List<string> output = new List<string>();
        //output = await informationProcessor.GetCollectionPropertiesForSale(177, "PRICE", "ALL", "TXT");
        //output = forSaleProcessor.GetCityPropertiesForSale(17, "Price", "All", "TXT");
        //output = await informationProcessor.GetNeighborhoodPropertiesForSale(235, "Price", "All");
        //output = forSaleProcessor.GetBuildingPropertiesForSale("City", 3, "price", "all", "CSV");
        //output = informationProcessor.GetCityInformation("TXT"); 
        //output = informationProcessor.GetAllProperties("Neighborhood", 1482, "CSV");
        //output = await informationProcessor.GetStreetPropertiesForSale(28029, "MARKUP", "ALL", "CSV");
        //output = await informationProcessor.GetAssetsByTypeAndUserName("NFLPA", "stoney300", "txt");
        //output = await informationProcessor.GetPropertyInfo("loyldoyl", "TXT");
        //output = await informationProcessor.GetBuildingsUnderConstruction(1);
        //List<SaleHistoryQueryEntry> entries = localDataManager.GetSaleHistoryByPropertyId(79565478804919);
        //output = informationProcessor.GetSaleHistoryByType("Property", "10, 9843 S Exchange Ave", "txt");
        //output = informationProcessor.SearchProperties(10, "3101 W", "TXT");
        //output = forSaleProcessor.GetBuildingPropertiesForSale("city", 0, "Price", "all", "txt");
        //output = informationProcessor.GetAllProperties("NEIGHBORHOOD", 1300, "TXT");
        //output = await profileAppraiser.RunAppraisal("hornbrod", "TXT");
        //output = informationProcessor.GetUnmintedProperties("CITY", 15, "NONFSA", "TXT");
        //output = forSaleProcessor.GetUsernamePropertiesForSale("sothbys", "Price", "all", "txt");
        //output = forSaleProcessor.GetCollectionPropertiesForSale(223, "Price", "all", "txt");
        //await File.WriteAllTextAsync(@"C:\Users\chri1\Desktop\fanpointleaders.csv", string.Join(Environment.NewLine, output));

        // Test Repo Actions
        //List<NFLPALegit> nflpaLegits = await uplandApiManager.GetNFLPALegitsByUsername("teeem");
        //UserProfile profile = await webProcessor.GetWebUIProfile("koraseph");

        // Rebuild Property Structure List
        //informationProcessor.RebuildPropertyStructures();
        //await informationProcessor.RunCityStatusUpdate();
        //await informationProcessor.RefreshCityById("PART", 1);

        //UserProfile profile = await webProcessor.GetWebUIProfile("hornbrod");

        List<NFTMetadata> meta = localDataManager.GetAllNFTMetadata().Where(m => m.Category == "structure").ToList();
        foreach (NFTMetadata m in meta)
        {
            StructureMetadata s = JsonSerializer.Deserialize<StructureMetadata>(Encoding.UTF8.GetString(m.Metadata));
            m.Metadata = Encoding.UTF8.GetBytes(JsonSerializer.Serialize<StructureMetadata>(s));

            localDataManager.UpsertNftMetadata(m);
        }
    }

    static void Main(string[] args) 
        => new Program().RunBotAsync().GetAwaiter().GetResult();
    
    public async Task RunBotAsync()
    {
        // DEBUG FUNCTION Uncomment to run local debug actions
        //await DebugFunction(); return;

        //DEBUG FUNCTION Sync Local Database to the Blockchain
        await ResyncLocalDatabase(); return;
        
        _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _client = new DiscordSocketClient();
        _commands = new CommandService();

        _services = new ServiceCollection()
            .AddSingleton<IUplandApiRepository, UplandApiRepository>()
            .AddSingleton<IUplandApiManager, UplandApiManager>()
            .AddSingleton<ILocalDataRepository, LocalDataRepository>()
            .AddSingleton<ICachingProcessor, CachingProcessor>()
            .AddSingleton<ILocalDataManager, LocalDataManager>()
            .AddSingleton<IBlockChainRepository, BlockchainRepository>()
            .AddSingleton<IBlockchainManager, BlockchainManager>()
            .AddSingleton<IProfileAppraiser, ProfileAppraiser>()
            .AddSingleton<IMappingProcessor, MappingProcessor>()
            .AddSingleton<IInformationProcessor, InformationProcessor>()
            .AddSingleton<IForSaleProcessor, ForSaleProcessor>()
            .AddSingleton<IPlayUplandMeSurfer, PlayUplandMeSurfer>()
            .AddSingleton<IUSPKTokenAccSurfer, USPKTokenAccSurfer>()
            .AddSingleton<IUplandNFTActSurfer, UplandNFTActSurfer>()
            .AddSingleton<IResyncProcessor, ResyncProcessor>()
            .AddSingleton(_client)
            .AddSingleton(_commands)
            .AddSingleton(_configuration)
            .BuildServiceProvider();

        _client.Log += clientLog;

        InitializeRefreshTimer();
        InitializeBlockchainUpdateTimer();

        await RegisterCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, _configuration["AppSettings:DiscordBotToken"]);

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
                            ((LocalDataManager)_services.GetService(typeof(LocalDataManager))).CreateErrorLog("Program.cs - HandleCommandAsync", result.ErrorReason);
                            break;
                        case "The server responded with error 40005: Request entity too large":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: That file exceeds the size limit set discord unfortunaley. Try requesting it as a CSV instead."));
                            break;
                        case "Timeout expired.  The timeout period elapsed prior to completion of the operation or the server is not responding.":
                            await context.Channel.SendMessageAsync(string.Format("ERROR: This command timed out for some reason. The hampster running the server probably just got tuckered out. Try again later."));
                            ((LocalDataManager)_services.GetService(typeof(LocalDataManager))).CreateErrorLog("Program.cs - HandleCommandAsync - Timeout", result.ErrorReason);
                            break;
                        default:
                            await context.Channel.SendMessageAsync(string.Format("ERROR: Contact Grombrindal."));
                            ((LocalDataManager)_services.GetService(typeof(LocalDataManager))).CreateErrorLog("Program.cs - HandleCommandAsync - Default", result.ErrorReason);
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
        _refreshTimer.Interval = 3600000; // Every hour
        _refreshTimer.Start();
    }

    private void InitializeBlockchainUpdateTimer()
    {
        _blockchainUpdateTimer = new Timer();
        _blockchainUpdateTimer.Elapsed += (sender, e) =>
        {
            Task child = Task.Factory.StartNew(async () =>
            {
                await _services.GetService<IPlayUplandMeSurfer>().RunBlockChainUpdate();
                await _services.GetService<IUplandNFTActSurfer>().RunBlockChainUpdate();
                await _services.GetService<IUSPKTokenAccSurfer>().RunBlockChainUpdate();
            });
        };
        _blockchainUpdateTimer.Interval = 30000; // Every 30 Seconds
        _blockchainUpdateTimer.Start();
    }

    private async Task RunRefreshActions()
    { 
        DateTime time = DateTime.UtcNow;

        // Rebuild the structure table every hour
        _services.GetService<IInformationProcessor>().RebuildPropertyStructures();

        if (time.Hour == 6)
        {
            // Checked the locked props every night.
            await ProcessResyncAction("CheckLocked", "1");

            // Run Some Cleanup actions on the properties

            // Check the locked props each night incase they have been unlocked
            //await ProcessResyncAction("CheckLocked", "1");

            // Clear Duplicate For Sale Entries
            //await ProcessResyncAction("ClearDupeForSale", "1");

            // Resync Unminted City
            //await ProcessResyncAction("CityUnmintedResync", "0");

            // Resync Unminted City Non FSA
            //await ProcessResyncAction("CityUnmintedResync", "-1");

            // Check Buildings For Sale
            //await ProcessResyncAction("BuildingSaleResync", "-1");

            // Check Neighborhood For Sale
            //await ProcessResyncAction("NeighborhoodSaleResync", "0");

            // Check Collection For Sale
            //await ProcessResyncAction("CollectionSaleResync", "0");
        }
    }

    private async Task ProcessResyncAction(string actionType, string list)
    {
        try
        {
            Console.WriteLine(string.Format("{0}: {1} Resync", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), actionType));
            await _services.GetService<IResyncProcessor>().ResyncPropsList(actionType, list);
            Console.WriteLine(string.Format("{0}: {1} Complete", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), actionType));
        }
        catch (Exception ex)
        {
            _services.GetService<ILocalDataManager>().CreateErrorLog(string.Format("Program.cs - {0}", actionType), ex.Message);
            Console.WriteLine(string.Format("{0}: {1} Resync: {2}", string.Format("{0:MM/dd/yy H:mm:ss}", DateTime.Now), actionType, ex.Message));
        }
    }

    private static async Task ResyncLocalDatabase()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(path: "appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        LocalDataRepository localDataRepository = new LocalDataRepository(configuration);
        UplandApiRepository uplandApiRepository = new UplandApiRepository(configuration);

        LocalDataManager localDataManager = new LocalDataManager(uplandApiRepository, localDataRepository);
        UplandApiManager uplandApiManager = new UplandApiManager(uplandApiRepository);
        BlockchainManager blockchainManager = new BlockchainManager();

        PlayUplandMeSurfer playUplandMeSurfer = new PlayUplandMeSurfer(localDataManager, uplandApiManager, blockchainManager, configuration);
        USPKTokenAccSurfer uspkTokenAccSurfer = new USPKTokenAccSurfer(localDataManager, blockchainManager, configuration);
        UplandNFTActSurfer uplandNFTActSurfer = new UplandNFTActSurfer(localDataManager, uplandApiManager, blockchainManager, configuration);
        InformationProcessor informationProcessor = new InformationProcessor(localDataManager, uplandApiManager, blockchainManager);
        ResyncProcessor resyncProcessor = new ResyncProcessor(localDataManager, uplandApiManager, uplandNFTActSurfer);

        await playUplandMeSurfer.RunBlockChainUpdate();
        await uplandNFTActSurfer.RunBlockChainUpdate();
        await uspkTokenAccSurfer.RunBlockChainUpdate();
        informationProcessor.RebuildPropertyStructures();
        await resyncProcessor.ResyncPropsList("ReloadMissingNFTs", "1");
    }
}