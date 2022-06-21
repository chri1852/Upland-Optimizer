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

class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;
    private IConfiguration _configuration;

    private Timer _refreshTimer;
    private Timer _blockchainUpdateTimer;

    /*
    static async Task Main(string[] args) // DEBUG FUNCTION
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
        PlayUplandMeSurfer playUplandMeSurfer = new PlayUplandMeSurfer(localDataManager, uplandApiManager, blockchainManager);
        USPKTokenAccSurfer uspkTokenAccSurfer = new USPKTokenAccSurfer(localDataManager, blockchainManager);
        UplandNFTActSurfer uplandNFTActSurfer = new UplandNFTActSurfer(localDataManager, uplandApiManager, blockchainManager);
        ForSaleProcessor forSaleProcessor = new ForSaleProcessor(localDataManager);
        InformationProcessor informationProcessor = new InformationProcessor(localDataManager, uplandApiManager, blockchainManager);
        //ProfileAppraiser profileAppraiser = new ProfileAppraiser(localDataManager, uplandApiManager, cachingProcessor);
        ResyncProcessor resyncProcessor = new ResyncProcessor(localDataManager, uplandApiManager, uplandNFTActSurfer);
        //MappingProcessor mappingProcessor = new MappingProcessor(localDataManager, profileAppraiser, cachingProcessor);
        WebProcessor webProcessor = new WebProcessor(localDataManager, uplandApiManager, cachingProcessor);
        CollectionOptimizer collectionOptimizer = new CollectionOptimizer(localDataManager);

        // Populate City
        //List<double> cityCoordinates = Upland.InformationProcessor.HelperFunctions.GetCityAreaCoordinates(16);
        //await localDataManager.PopulateAllPropertiesInArea(cityCoordinates[0], cityCoordinates[1], cityCoordinates[2], cityCoordinates[3], 16, false);
        //localDataManager.DetermineNeighborhoodIdsForCity(16);
        //await localDataManager.PopulateCollectionPropertiesByCityId(16);
        //await informationProcessor.LoadMissingCityProperties(35);

        //await resyncProcessor.ResyncPropsList("EnclaveFix", "1");
        //await localDataManager.PopulateDatabaseCollectionInfo(10);
        //new Program().InitializeRefreshTimer();

        /// Test Optimizer
        //OptimizerRunRequest runRequest = new OptimizerRunRequest("marsbuggy", 7, true);
        //await collectionOptimizer.RunAutoOptimization(new RegisteredUser(), runRequest);

        // Populate initial City Data
        //await localDataManager.PopulateNeighborhoods();
        //await localDataManager.PopulateDatabaseCollectionInfo();
        //await localDataManager.PopulateStreets();
        //await informationProcessor.LoadMissingCityProperties(35);
        
        string continueHunt = "Y";
        while (continueHunt == "Y")
        {
            await informationProcessor.HuntTreasures(3, "oqtr232h2c23", TreasureTypeEnum.Rush);
            Console.WriteLine("Continue?");
            continueHunt = Console.ReadLine();
        }
        
        // Run Blockchain Updates

        //await localDataManager.PopulateDatabaseCollectionInfo(35);

        await playUplandMeSurfer.RunBlockChainUpdate();
        await uplandNFTActSurfer.RunBlockChainUpdate();
        await uspkTokenAccSurfer.RunBlockChainUpdate();
        //informationProcessor.RebuildPropertyStructures();
        //await resyncProcessor.ResyncPropsList("ReloadMissingNFTs", "1");

        // Test Information Processing Functions
        List<string> output;
        //output = webProcessor.GetFanPointsLeaders();
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

        // Dictionary<string, double> stakes = await blockchainManager.GetStakedSpark();

        // List<KeyValuePair<string, double>> list = stakes.ToList().OrderByDescending(s => s.Value).ToList();
        //localDataManager.UpsertConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES, true.ToString());

        //await blockchainRepository.GetCleosActions(0, "playuplandme");
        //await localDataManager.PopulateDatabaseCollectionInfo(4);
        //List<EOSFlareAction> actions = await blockchainManager.GetEOSFlareActions(0);
        //await informationProcessor.LoadMissingCityProperties(4);
        //informationProcessor.RebuildPropertyStructures();
        //List<UIPropertyHistory> history = webProcessor.GetPropertyHistory(82363818741988);
        //await playUplandMeSurfer.BuildBlockChainFromBegining();
        //await resyncProcessor.ResyncPropsList("SetMonthlyEarnings", "79534961051253,79521388283491,81837349780461,78004661005426,78888484689652,78904959913211,79511707830036,79511774938900,79511842047764,79511925933844,79511993042708,79512060151572,79512194369300,79512261478163,79512395695891,79512462804755,79512529913619,79513838536647,79514056640437,79521304397411,79530783523022,79534877162987,79534877162990,79534877162992,79535481145672,79535615362618,79548282158203,79549087465212,79549137796863,79549171351295,79555496364296,79565126483166,81311015210304,81315041744633,81315477947821,81327322665662,81328329293407,81328664837833,81343160354735,81347757313096,81365943814126,82055906393018,82070771004481,79530934518624,79506909546712,81302005842934");
        //await resyncProcessor.ResyncPropsList("SetMinted", "79518905257767,79518989142699,79518703931192,79523250555190,79532444469250,79547644626869,79519526013600,79520213880349,79520146771483,79519509236382,79561905259692,78984920124378,78887310285931,78929068769844,79517227535145,79518133504761,79518318061598,79518401940169,79518401940172,79519073030500,79520012553749,79520029330975,79520062885396,79520062885406,79520113217052,79523468658980,79532209585139,79534273183806,79538266160744,79539138584510,79539725779717,79548718366259,79552057033426,79553214661391,79553264993038,79553533428547,79553600537166,79554338735909,79554909160374,79556670768730,79561385165513,79564220514486,81305646501381,81306888017333,81334973077200,81341197419348,81351179866241,81351565742204,81351699959931,81372017166497,81383140456110,81407685523534,78985004010446");
        //await resyncProcessor.ResyncPropsList("CheckLocked", "-1");

        //mappingProcessor.CreateMap(9, "BUILDINGS", 1, false, new List<string>());

        //UserProfile profile = await webProcessor.GetWebUIProfile("hornbrod");
        
        WebNFTFilters filters = new WebNFTFilters
        {
            IncludeBurned = false,
            SortBy = "Mint",
            SortDescending = false,
            Category = Consts.METADATA_TYPE_ESSENTIAL,
            PageSize = 100,
            Page = 1,
            NoPaging = false,
            Filters = new WebNFT
            {
                Year = "2021",
                Team = "Green Bay",
                Name = ""
            }
        };

        //List<WebNFT> nfts = webProcessor.SearchNFTs(filters);
        
        //AppraisalResults results = await profileAppraiser.RunAppraisal(new RegisteredUser { Id = 1, UplandUsername = "hornbrod" });
        //await File.WriteAllTextAsync(@"C:\Users\chri1\Desktop\hornbrod.csv", string.Join(Environment.NewLine, profileAppraiser.BuildAppraisalCsvStrings(results)));
        //mappingProcessor.SaveMap(mappingProcessor.CreateMap(13, "PERUP2", false), "test123");
        //mappingProcessor.CreateMap(12, "Buildings", 1, false);
     }
    */
    ///*
    static void Main(string[] args) 
        => new Program().RunBotAsync().GetAwaiter().GetResult();
    //*/

    public async Task RunBotAsync()
    {
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
}