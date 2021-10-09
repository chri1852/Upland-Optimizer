using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
//using Upland.CollectionOptimizer;  // ONLY UNCOMMENT FOR DEBUGING
//using Upland.Infrastructure.LocalData;  // ONLY UNCOMMENT FOR DEBUGING

class Program
{
    private DiscordSocketClient _client;
    private CommandService _commands;
    private IServiceProvider _services;

    /*
    static async Task Main(string[] args) // DEBUG FUNCTION
    {
        CollectionOptimizer collectionOptimizer = new CollectionOptimizer();
        string username;
        string qualityLevel;

        LocalDataManager localDataManager = new LocalDataManager();
        await localDataManager.PopulateDatabaseCollectionInfo();

        //Console.Write("Enter the Upland Username: ");
        //username = Console.ReadLine();
        //Console.Write("Enter the Level (1-8)....: ");
        //qualityLevel = Console.ReadLine();


        // await collectionOptimizer.RunDebugOptimization(username, int.Parse(qualityLevel));
    }
    */
    
    static void Main(string[] args) 
        => new Program().RunBotAsync().GetAwaiter().GetResult();
    
    public async Task RunBotAsync()
    {
        _client = new DiscordSocketClient();
        _commands = new CommandService();

        _services = new ServiceCollection()
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
            var result = await _commands.ExecuteAsync(context, argPos, _services);
            if (!result.IsSuccess) Console.WriteLine(result.ErrorReason);
        }
    }
}