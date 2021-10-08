using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Infrastructure.LocalData;
using Upland.Infrastructure.UplandApi;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Startup.Commands
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;

        public Commands()
        {
            _random = new Random();
        }

        [Command("Ping")]
        public async Task Ping()
        {
            int rand = _random.Next(0, 8);
            switch (rand)
            {
                case 0:
                    await ReplyAsync(string.Format("Knock it off {0}!", HelperFunctions.GetRandomName(_random)));
                    break;
                case 1:
                    await ReplyAsync(string.Format("Cool it {0}!", HelperFunctions.GetRandomName(_random)));
                    break;
                case 2:
                    await ReplyAsync(string.Format("Put a sock in it {0}!", HelperFunctions.GetRandomName(_random)));
                    break;
                case 3:
                    await ReplyAsync(string.Format("Quit it {0}!", HelperFunctions.GetRandomName(_random)));
                    break;
                case 4:
                    await ReplyAsync(string.Format("Easy there {0}!", HelperFunctions.GetRandomName(_random)));
                    break;
                case 5:
                    await ReplyAsync(string.Format("Dial it back {0}!", HelperFunctions.GetRandomName(_random)));
                    break;
                case 6:
                    await ReplyAsync(string.Format("Cool your jets {0}!", HelperFunctions.GetRandomName(_random)));
                    break;
                case 7:
                    await ReplyAsync(string.Format("Calm down {0}!", HelperFunctions.GetRandomName(_random)));
                    break;
            }
        }

        [Command("RegisterMe")]
        public async Task RegisterMe(string uplandUserName)
        {
            LocalDataManager localDataManager = new LocalDataManager();
            UplandApiRepository uplandApiRepository = new UplandApiRepository();
            List<UplandAuthProperty> properties;

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (registeredUser != null && registeredUser.DiscordUsername != null && registeredUser.DiscordUsername != "")
            {
                if (registeredUser.Verified)
                {
                    await ReplyAsync(string.Format("Looks like you already registered and verified {0}.", registeredUser.UplandUsername));
                }
                else
                {
                    properties = await uplandApiRepository.GetPropertysByUsername(registeredUser.UplandUsername);
                    await ReplyAsync(string.Format("Looks like you already registered {0}. The way I see it you have two choices.", registeredUser.UplandUsername));
                    await ReplyAsync(string.Format("1. Place the property at {0}, for sale for {1:N2}UPX, and then use my !VerifyMe command. Or...", properties.Where(p => p.Prop_Id == registeredUser.PropertyId).First().Full_Address, registeredUser.Price));
                    await ReplyAsync(string.Format("2. Run my !ClearMe command to clear your unverified registration, and register again with !RegisterMe."));
                }
                return;
            }

            properties = await uplandApiRepository.GetPropertysByUsername(uplandUserName.ToLower());
            if (properties == null || properties.Count == 0)
            {
                await ReplyAsync(string.Format("Looks like {0} is not a player {1}.", uplandUserName, HelperFunctions.GetRandomName(_random)));
                return;
            }

            UplandAuthProperty verifyProperty = properties[_random.Next(0, properties.Count)];
            int verifyPrice = _random.Next(80000000, 90000000);

            RegisteredUser newUser = new RegisteredUser()
            {
                DiscordUserId = Context.User.Id,
                DiscordUsername = Context.User.Username,
                UplandUsername = uplandUserName.ToLower(),
                PropertyId = verifyProperty.Prop_Id,
                Price = verifyPrice
            };

            try
            {
                localDataManager.CreateRegisteredUser(newUser);
            }
            catch
            {
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            await ReplyAsync(string.Format("Good News {0}! I have registered you as a user!", HelperFunctions.GetRandomName(_random)));
            await ReplyAsync(string.Format("To continue place, {0}, up for sale for {1:N2} UPX, and then use my !VerifyMe command.", verifyProperty.Full_Address, verifyPrice));
        }

        [Command("ClearMe")]
        public async Task ClearMe()
        {
            LocalDataManager localDataManager = new LocalDataManager();

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (registeredUser != null && registeredUser.DiscordUsername != null && registeredUser.DiscordUsername != "")
            {
                if (registeredUser.Verified)
                {
                    await ReplyAsync(string.Format("Looks like you are already verified {0}. Try contacting Grombrindal.", HelperFunctions.GetRandomName(_random)));
                }
                else
                {
                    try
                    {
                        localDataManager.DeleteRegisteredUser(Context.User.Id);
                        await ReplyAsync(string.Format("I got you {0}. I have cleared your registration. Try again with my !RegisterMe command with your Upland username", HelperFunctions.GetRandomName(_random)));
                    }
                    catch
                    {
                        await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                        return;
                    }
                }
                return;
            }

            await ReplyAsync(string.Format("You don't appear to exist {0}. Try again with my !RegisterMe command with your Upland username", HelperFunctions.GetRandomName(_random)));
        }

        [Command("VerifyMe")]
        public async Task VerifyMe()
        {
            LocalDataManager localDataManager = new LocalDataManager();
            UplandApiRepository uplandApiRepository = new UplandApiRepository();

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("You don't appear to exist {0}. Try again with my !RegisterMe command with your Upland username", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (registeredUser.Verified)
            {
                await ReplyAsync(string.Format("Looks like you are already verified {0}.", HelperFunctions.GetRandomName(_random)));
            }
            else
            {
                UplandProperty property = await uplandApiRepository.GetPropertyById(registeredUser.PropertyId);
                if (property.on_market == null)
                {
                    await ReplyAsync(string.Format("Doesn't look like {0} is on sale for {1:N2}.", property.Full_Address, registeredUser.Price));
                    return;
                }

                if (property.on_market.token != string.Format("{0}.00 UPX", registeredUser.Price))
                {
                    await ReplyAsync(string.Format("{0} is on sale, but it not for {1:N2}", property.Full_Address, registeredUser.Price));
                    return;
                }
                else
                {
                    localDataManager.SetRegisteredUserVerified(registeredUser.DiscordUserId);
                    await ReplyAsync(string.Format("You are now Verified {0}! You can remove the property from sale, or don't. I'm not your dad.", HelperFunctions.GetRandomName(_random)));
                }
            }
            return;
        }

        [Command("OptimizerRun")]
        public async Task OptimizerRun()
        {
            LocalDataManager localDataManager = new LocalDataManager();

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            if (!registeredUser.Paid && registeredUser.RunCount > Consts.WarningRuns && registeredUser.RunCount < Consts.FreeRuns)
            {
                await ReplyAsync(string.Format("You've used {0} out of {1} of your free runs {2}. To learn how to support this tool try my !SupportMe command.", registeredUser.RunCount, Consts.FreeRuns, HelperFunctions.GetRandomName(_random)));
            }
            else if (!registeredUser.Paid && registeredUser.RunCount == Consts.FreeRuns)
            {
                await ReplyAsync(string.Format("You've used all {0} of your free runs {1}. To learn how to support this tool try my !SupportMe command.", Consts.FreeRuns, HelperFunctions.GetRandomName(_random)));
                return;
            }

            OptimizationRun currentRun = localDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);
            if (currentRun != null && currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("You alread have a run in progress {0}. Try using my !OptimizerStatus command to track its progress.", HelperFunctions.GetRandomName(_random)));
                return;
            }
            
            if (currentRun != null)
            {
                localDataManager.DeleteOptimizerRuns(registeredUser.DiscordUserId);
            }

            try
            {
                Task child = Task.Factory.StartNew(async () =>
                {
                    CollectionOptimizer optimizer = new CollectionOptimizer();
                    await optimizer.RunAutoOptimization(registeredUser, 7);
                });

                await ReplyAsync(string.Format("Got it {0}! I have started your optimization run.", HelperFunctions.GetRandomName(_random)));
                return;
            }
            catch
            {
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }
        }

        [Command("OptimizerStatus")]
        public async Task OptimizerStatus()
        {
            LocalDataManager localDataManager = new LocalDataManager();

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = localDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);
            if (currentRun != null)
            {
                await ReplyAsync(string.Format("Got it {0}. Your current run has a status of {1}.", HelperFunctions.GetRandomName(_random), currentRun.Status));
                return;
            }
            else
            {
                await ReplyAsync(string.Format("I don't see any optimization runs for you {0}. Try using my !OptimizerRun command to run one.", HelperFunctions.GetRandomName(_random)));
                return;
            }
        }

        [Command("OptimizerResults")]
        public async Task OptimizerResults()
        {
            LocalDataManager localDataManager = new LocalDataManager();

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = localDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);
            if (currentRun == null)
            {
                await ReplyAsync(string.Format("I don't see any optimization runs for you {0}. Try using my !OptimizerRun command to run one.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("Looks like your optimizer run is still running {0}.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (currentRun.Status == Consts.RunStatusFailed)
            {
                await ReplyAsync(string.Format("Looks like your optimizer run failed {0}. You can try running it again, or ping Grombrindal for help.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (currentRun.Status == Consts.RunStatusCompleted)
            {
                await ReplyAsync(string.Format("I got you {0}. Let me post those results for you.{1}", HelperFunctions.GetRandomName(_random), Environment.NewLine));

                List<string> results = Encoding.UTF8.GetString(currentRun.Results).Split(Environment.NewLine).ToList();
                string message = "";
                foreach (string entry in results)
                {
                    if(message.Length + entry.Length + 1 < 2000)
                    {
                        message += entry;
                        message += Environment.NewLine;
                    }
                    else
                    {
                        await ReplyAsync(string.Format("{0}", message));
                        message = entry;
                        message += Environment.NewLine;
                    }
                }

                await ReplyAsync(string.Format("{0}", message));

                return;
            }
        }

        [Command("SupportMe")]
        public async Task SupportMe()
        {
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            if (registeredUser.Paid)
            {
                await ReplyAsync(string.Format("You are already a supporter {0}. Thanks for helping out!", HelperFunctions.GetRandomName(_random)));
            }
            else
            {
                await ReplyAsync(string.Format("Hey {0}, Sounds like you really like this tool, to help support this tool why don't you ping Grombrindal.", HelperFunctions.GetRandomName(_random)));
                await ReplyAsync(string.Format("For the low price of $5 you will get perpetual access to run this when ever you like, access to new premium features, and get a warm fuzzy feeling knowing you are helping to pay for hosting and development costs.", HelperFunctions.GetRandomName(_random)));
                await ReplyAsync(string.Format("USD, UPX, Waxp, Ham Sandwiches, MTG Bulk Rares, and more are all accepted in payment.", HelperFunctions.GetRandomName(_random)));
            }
        }

        [Command("Help")]
        public async Task Help()
        {
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            UplandApiRepository uplandApiRepository = new UplandApiRepository();
            List<UplandAuthProperty> properties;

            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("Looks like you don't exist {0}. To start try running !RegisterMe with your Upland username!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (registeredUser != null && registeredUser.DiscordUsername != null && registeredUser.DiscordUsername != "")
            {
                if (!registeredUser.Verified)
                {
                    properties = await uplandApiRepository.GetPropertysByUsername(registeredUser.UplandUsername);
                    await ReplyAsync(string.Format("Looks like you have registered, but not verified yet {0}. The way I see it you have two choices.", registeredUser.UplandUsername));
                    await ReplyAsync(string.Format("1. Place the property at {0}, for sale for {1:N2}UPX, and then use my !VerifyMe command. Or...", properties.Where(p => p.Prop_Id == registeredUser.PropertyId).First().Full_Address, registeredUser.Price));
                    await ReplyAsync(string.Format("2. Run my !ClearMe command to clear your unverified registration, and register again with !RegisterMe."));
                    return;
                }
            }

            // They are registered now, display help
            if (!registeredUser.Paid)
            {
                await ReplyAsync(string.Format("Hello {0}! Everyone gets {1} free runs of the optimizer, you've used {2} of them. To learn how to support this tool try my !SupportMe command.{3}", HelperFunctions.GetRandomName(_random), Consts.FreeRuns, registeredUser.RunCount, Environment.NewLine));
            }
            else
            {
                await ReplyAsync(string.Format("Hey there {0}! Thanks for being a supporter!{1}", HelperFunctions.GetRandomName(_random), Environment.NewLine));
            }

            await ReplyAsync(string.Format("To Use the Collection Optimizer Simply run my !OptimizerRun command.{0}The Optimizer can take some time to run, especially if this is the first time you have run it.{0}You can check on the status of your run at anytime by running my !OptimizerStatus command.{0}Once the run has a status of {1}, you can run my !OptimizerResults command.{0}If your run has a status of failed you can try running it again, or reach out to Grombrindal for troubleshooting.{0}{0}Supports have access to my !OptimizerLevelRun command which allows you to specify the quality of your run. For the most part 7 is good for everyone, but using this command you can specify between 3 and 10. Note that 10 takes upwards of an hour or more to complete.", Environment.NewLine, Consts.RunStatusCompleted, Consts.RunStatusFailed));
        }

        private async Task<bool> EnsureRegisteredAndVerified(RegisteredUser registeredUser)
        {
            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("You don't appear to exist {0}. Try again with my !RegisterMe command with your Upland username!", HelperFunctions.GetRandomName(_random)));
                return false;
            }

            if (!registeredUser.Verified)
            {
                await ReplyAsync(string.Format("Looks like you are not verified {0}. Try Again with my !VerifyMe command.", HelperFunctions.GetRandomName(_random)));
                return false;
            }

            return true;
        }
    }
}
