using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Upland.InformationProcessor;

/*
// ONLY UNCOMMENT FOR DEBUGING
using Upland.CollectionOptimizer;  
using Upland.Infrastructure.LocalData;
using System.Collections.Generic;
using System.IO;
using Upland.Types.Types;
using System.Linq;
*/

class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;
    private InformationProcessor _informationProcessor;

    /*
    static async Task Main(string[] args) // DEBUG FUNCTION
    {
        LocalDataManager localDataManager = new LocalDataManager();
        CollectionOptimizer collectionOptimizer = new CollectionOptimizer();
        InformationProcessor informationProcessor = new InformationProcessor();

        string username;
        string qualityLevel;
        List<string> output = new List<string>();

        /// Test Optimizer
        //Console.Write("Enter the Upland Username: ");
        //username = Console.ReadLine();
        //Console.Write("Enter the Level (1-8)....: ");
        //qualityLevel = Console.ReadLine();
        //await collectionOptimizer.RunDebugOptimization(username, int.Parse(qualityLevel), 201, 20, 1000);

        // Populate initial City Data
        //await localDataManager.PopulateNeighborhoods();
        //await localDataManager.PopulateDatabaseCollectionInfo();

        // Test Information Processing Functions
        //output = await informationProcessor.GetCollectionPropertiesForSale(177, "PRICE", "ALL");
        //output = await informationProcessor.GetCollectionsSalesDataByCityId(0);
        output = await informationProcessor.GetNeighborhoodPropertiesForSale(235, "Price", "All");
        //await File.WriteAllTextAsync(@"C:\Users\chri1\Desktop\Upland\OptimizerBot\test_file.txt", string.Join(Environment.NewLine, output));

        // Populate CityProps And Neighborhoods
        //await localDataManager.PopulateAllPropertiesInArea(37.893514, 37.698581, -122.100490, -122.354549, 7);
        //localDataManager.DetermineNeighborhoodIdsForCity(14);
    }
    */
    
    static void Main(string[] args) 
        => new Program().RunBotAsync().GetAwaiter().GetResult();
    
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

        await RegisterCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, token);

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
        SocketCommandContext context = new SocketCommandContext(_client, message);
        if (message.Author.IsBot) return;

        int argPos = 0;
        if (message.HasStringPrefix("!", ref argPos))
        {
            Task child = Task.Factory.StartNew(async () =>
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);
                if (!result.IsSuccess)
                {
                    Console.WriteLine(result.ErrorReason);
                    Console.WriteLine(string.Format("{0}: {1}", message.Author.Username, message.Content));
                }
                else
                {
                    Console.WriteLine(string.Format("{0}: {1}", message.Author.Username, message.Content));
                }
            });
        }
    }
}