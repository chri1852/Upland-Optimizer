using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.InformationProcessor;
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
            await ReplyAsync(string.Format("To continue place, {0}, up for sale for {1:N2} UPX, and then use my !VerifyMe command. If you can't place the propery for sale run my !ClearMe command.", verifyProperty.Full_Address, verifyPrice));
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
                await ReplyAsync(string.Format("Roger that {0}. Your current run has a status of {1}.", HelperFunctions.GetRandomName(_random), currentRun.Status));
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

                byte[] resultBytes = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(currentRun.Results));
                using (Stream stream = new MemoryStream())
                {
                    stream.Write(resultBytes, 0, resultBytes.Length);
                    stream.Seek(0, SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(stream, string.Format("{0}_OptimizerResults.txt", registeredUser.UplandUsername));
                }
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
                await ReplyAsync(string.Format("Hey {0}, Sounds like you really like this tool, to help support this tool why don't you ping Grombrindal.{1}{1}", HelperFunctions.GetRandomName(_random), Environment.NewLine));
                await ReplyAsync(string.Format("For the low price of $5 you will get perpetual access to run this when ever you like, access to new premium features, and get a warm fuzzy feeling knowing you are helping to pay for hosting and development costs. USD, UPX, Waxp, Ham Sandwiches, MTG Bulk Rares, and more are all accepted in payment."));
            }
        }

        [Command("CollectionInfo")]
        public async Task CollectionInfo()
        {
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            InformationProcessor informationProcessor = new InformationProcessor();

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            List<string> collectionData = informationProcessor.GetCollectionInformation();

            await ReplyAsync(string.Format("Here you go {0}!{1}", HelperFunctions.GetRandomName(_random), Environment.NewLine));

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, collectionData));
            using(Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, "CollectionInfo.txt");
            }
        }

        [Command("PropertyInfo")]
        public async Task PropertyInfo()
        {
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            InformationProcessor informationProcessor = new InformationProcessor();

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = localDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);
            if (currentRun == null)
            {
                await ReplyAsync(string.Format("Run an Optimization Run first {0}.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            List<string> propertyData = await informationProcessor.GetMyPropertyInfo(registeredUser.UplandUsername);

            await ReplyAsync(string.Format("Here you go {0}!{1}", HelperFunctions.GetRandomName(_random), Environment.NewLine));

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, propertyData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, "PropertyInfo.txt");
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
                await ReplyAsync(string.Format("Hello {0}! Everyone gets {1} free runs of the optimizer, you've used {2} of them. To learn how to support this tool try my !SupportMe command.{3}{3}", HelperFunctions.GetRandomName(_random), Consts.FreeRuns, registeredUser.RunCount, Environment.NewLine));
            }
            else
            {
                await ReplyAsync(string.Format("Hey there {0}! Thanks for being a supporter!{1}{1}", HelperFunctions.GetRandomName(_random), Environment.NewLine));
            }

            List<string> helpMenu = new List<string>();

            helpMenu.Add("Below are the functions you can run use my !Help command and specify the number of the command you want more information on, like !Help 2.");
            helpMenu.Add("");
            helpMenu.Add("Standard Commands");
            helpMenu.Add("   1. !OptimizerRun");
            helpMenu.Add("   2. !OptimizerStatus");
            helpMenu.Add("   3. !OptimizerResults");
            helpMenu.Add("   4. !CollectionInfo");
            helpMenu.Add("   5. !PropertyInfo");
            helpMenu.Add("   6. !SupportMe");
            helpMenu.Add("");
            helpMenu.Add("Premium Commands");
            helpMenu.Add("   7. !OptimizerLevelRun");
            helpMenu.Add("   8. !OptimizerWhatIfRun");
            helpMenu.Add("");
            await ReplyAsync(string.Format("{0}", string.Join(Environment.NewLine, helpMenu)));
        }

        [Command("Help")]
        public async Task Help(string command)
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

            List<string> helpOutput = new List<string>();
            switch (command)
            {
                case "1":
                    helpOutput.Add(string.Format("!OptimizerRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will start an optimizer run for you. Standard users get 6 free runs, while premium users can run this as many times as they like. To get your results or check on the status run the !OptimizerResults or !OptimizerStatus commands. The first time your run the optimizer it may take some extra time as the system retrieves your property information from Upland."));
                    break;
                case "2":
                    helpOutput.Add(string.Format("!OptimizerStatus"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return the status of your current run, it can be either In Progress, Failed, or Completed. If it fails reach out to Grombrindal."));
                    break;
                case "3":
                    helpOutput.Add(string.Format("!OptimizerResults"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with the results of your optimizer run. It will also list off Unfilled Collections, which you can fill, but the algorithm decided not to, and Unoptimized Collections, which you own at least one property in, but not enough to fill them."));
                    break;
                case "4":
                    helpOutput.Add(string.Format("!CollectionInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with information on all collections. Note that the property count does not include any locked properties, city and standard collections will also have a property count of 0."));
                    break;
                case "5":
                    helpOutput.Add(string.Format("!PropertyInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with all of your properties."));
                    break;
                case "6":
                    helpOutput.Add(string.Format("!SupportMe"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will let you know how to support the development of this tool."));
                    break;
                case "7":
                    helpOutput.Add(string.Format("!OptimizerLevelRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will run an optimizer run with a level you specify between 3 and 10. Levels 9 and especially 10 can take quite some time to run. You can get the results and check the status with the standard !OptimizerStatus and !OptimizerResults commands."));
                    helpOutput.Add("EX: !OptimizerLevelRun 5");
                    break;
                case "8":
                    helpOutput.Add(string.Format("!OptimizerWhatIfRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will run an optimizer run with some additional fake properties in the requested collection. You will need to specify the collection Id to add the properties to, the number of properties to add, and the average monthly upx of the properties. You can get the results and check the status with the standard !OptimizerStatus and !OptimizerResults commands."));
                    helpOutput.Add("EX: !OptimizerWhatIfRun 188 3 250.10");
                    helpOutput.Add("The above command will run a WhatIfRun with your currenty properties, and 3 fake properties in the French Quarter collection with an average monthly upx earnings of 250.10 upx.");
                    break;
                default:
                    helpOutput.Add(string.Format("Not sure what command you are refering to {0}. Try running my !Help command.", HelperFunctions.GetRandomName(_random)));
                    break;
            }

            await ReplyAsync(string.Format("{0}", string.Join(Environment.NewLine, helpOutput)));
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
