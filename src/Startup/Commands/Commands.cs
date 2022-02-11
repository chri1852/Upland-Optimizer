using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Upland.CollectionOptimizer;
using Upland.Interfaces.Repositories;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;
using System.Text.Json;

namespace Startup.Commands
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        private readonly Random _random;
        private readonly IInformationProcessor _informationProcessor;
        private readonly ILocalDataManager _localDataManager;
        private readonly IProfileAppraiser _profileAppraiser;
        private readonly IForSaleProcessor _forSaleProcessor;
        private readonly IMappingProcessor _mappingProcessor;
        private readonly IUplandApiRepository _uplandApiRepository;

        public Commands(IInformationProcessor informationProcessor, ILocalDataManager localDataManager, IProfileAppraiser profileAppraiser, IForSaleProcessor forSaleProcessor, IMappingProcessor mappingProcessor, IUplandApiRepository uplandApiRepository)
        {
            _random = new Random();
            _informationProcessor = informationProcessor;
            _localDataManager = localDataManager;
            _profileAppraiser = profileAppraiser;
            _forSaleProcessor = forSaleProcessor;
            _mappingProcessor = mappingProcessor;
            _uplandApiRepository = uplandApiRepository;
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
            List<UplandAuthProperty> properties;
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUserByUplandUsername(uplandUserName.ToLower());

            properties = await _uplandApiRepository.GetPropertysByUsername(uplandUserName.ToLower());
            if (properties == null || properties.Count == 0)
            {
                await ReplyAsync(string.Format("Looks like {0} is not a player or has no properties {1}.", uplandUserName, HelperFunctions.GetRandomName(_random)));
                return;
            }

            List<Property> localProps = _localDataManager.GetProperties(properties.Select(p => p.Prop_Id).ToList());
            int verifyPrice = 0;
            Property verifyProperty = null;
            int nonFSACount = localProps.Where(p => !p.FSA).ToList().Count;

            if (nonFSACount > 0)
            {
                verifyProperty = localProps.Where(p => !p.FSA).ToList()[_random.Next(0, nonFSACount)];
                verifyPrice = _random.Next(80000000, 90000000);
            }
            else
            {
                verifyProperty = localProps[_random.Next(0, localProps.Count)];
                verifyPrice = _random.Next(80000000, 90000000);
            }

            // A registered user exists for this username, lets see if it is a web or discord or both
            if (registeredUser != null)
            {
                // A discord entry appears to already exist
                if (registeredUser.DiscordUserId != null)
                {
                    // The registered User exists, and matches the discord userId
                    if (registeredUser.DiscordUserId == Context.User.Id)
                    {
                        if (registeredUser.DiscordVerified)
                        {
                            await ReplyAsync(string.Format("Looks like you already registered and verified {0}.", registeredUser.UplandUsername));
                            return;
                        }
                        else
                        {
                            // They exist and match, but havent Verified, and the verification has not expired
                            if (registeredUser.VerifyExpirationDateTime > DateTime.UtcNow)
                            {
                                properties = await _uplandApiRepository.GetPropertysByUsername(registeredUser.UplandUsername);
                                await ReplyAsync(string.Format("Looks like you already registered {0}. The way I see it you have two choices.", registeredUser.UplandUsername));
                                await ReplyAsync(string.Format("1. Place the property at {0}, for sale for {1:N2}UPX, and then use my !VerifyMe command. Or...", properties.Where(p => p.Prop_Id == registeredUser.PropertyId).First().Full_Address, registeredUser.Price));
                                await ReplyAsync(string.Format("2. Run my !ClearMe command to clear your unverified registration, and register again with !RegisterMe."));
                                return;
                            }
                        }
                    }
                    // They do not match the discordId, curious
                    else
                    {
                        if (registeredUser.DiscordVerified || (registeredUser.VerifyType == Consts.VERIFYTYPE_DISCORD && registeredUser.VerifyExpirationDateTime < DateTime.UtcNow))
                        {
                            await ReplyAsync(string.Format("Not sure what you are tying to pull here {0}. {1} is already registered under a different person.", HelperFunctions.GetRandomName(_random), registeredUser.UplandUsername));
                            return;
                        }
                        else
                        {
                            await ReplyAsync(string.Format("Strange, someone else claimed that username but never verified. I guess I'll give you a chance {0}", HelperFunctions.GetRandomName(_random)));
                        }
                    }
                }
                // This must be a web account
                else
                {
                    // They have not web verified, but are in the process
                    if (!registeredUser.WebVerified && registeredUser.VerifyType == Consts.VERIFYTYPE_WEB && registeredUser.VerifyExpirationDateTime < DateTime.UtcNow)
                    {
                        await ReplyAsync("Looks like someone is in the process of verifying this username on the web, better finish that first.");
                        return;
                    }
                }

                registeredUser.DiscordUserId = Context.User.Id;
                registeredUser.DiscordUsername = Context.User.Username;
                registeredUser.VerifyType = Consts.VERIFYTYPE_DISCORD;
                registeredUser.VerifyExpirationDateTime = DateTime.UtcNow.AddMinutes(30);
                registeredUser.Price = verifyPrice;
                registeredUser.PropertyId = verifyProperty.Id;
                _localDataManager.UpdateRegisteredUser(registeredUser);
            }
            // The registered user is null
            else
            {
                _localDataManager.CreateRegisteredUser(new RegisteredUser()
                {
                    DiscordUserId = Context.User.Id,
                    DiscordUsername = Context.User.Username,
                    UplandUsername = uplandUserName.ToLower(),
                    PropertyId = verifyProperty.Id,
                    Price = verifyPrice,
                    VerifyType = Consts.VERIFYTYPE_DISCORD,
                    VerifyExpirationDateTime = DateTime.UtcNow.AddMinutes(30)
                });
            }

            await ReplyAsync(string.Format("Good News {0}! I have registered you as a user!", HelperFunctions.GetRandomName(_random)));
            await ReplyAsync(string.Format("To continue place, {0}, {1}, up for sale for {2:N2} UPX, and then use my !VerifyMe command in the next 30 minutes. If you can't place the propery for sale run my !ClearMe command.", verifyProperty.Address, Consts.Cities[verifyProperty.CityId], verifyPrice));
        }

        [Command("ClearMe")]
        public async Task ClearMe()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            // They have not web verified, but are in the process
            if (!registeredUser.WebVerified && registeredUser.VerifyType == Consts.VERIFYTYPE_WEB && registeredUser.VerifyExpirationDateTime < DateTime.UtcNow)
            {
                await ReplyAsync("Looks like someone is in the process of verifying this username on the web, better finish that first.");
                return;
            }

            if (registeredUser != null && registeredUser.DiscordUsername != null && registeredUser.DiscordUsername != "")
            {
                if (registeredUser.DiscordVerified)
                {
                    await ReplyAsync(string.Format("Looks like you are already verified {0}.", HelperFunctions.GetRandomName(_random)));
                }
                else if (registeredUser.VerifyType == null || registeredUser.VerifyType == Consts.VERIFYTYPE_DISCORD)
                {
                    try
                    {
                        registeredUser.Price = 0;
                        registeredUser.PropertyId = 0;
                        registeredUser.VerifyType = null;
                        registeredUser.DiscordUserId = null;
                        registeredUser.DiscordUsername = null;
                        _localDataManager.UpdateRegisteredUser(registeredUser);
                        await ReplyAsync(string.Format("I got you {0}. I have cleared your registration. Try again with my !RegisterMe command with your Upland username", HelperFunctions.GetRandomName(_random)));
                    }
                    catch (Exception ex)
                    {
                        _localDataManager.CreateErrorLog("Commands - ClearMe", ex.Message);
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
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("You don't appear to exist {0}. Try again with my !RegisterMe command with your Upland username", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (registeredUser.DiscordVerified)
            {
                await ReplyAsync(string.Format("Looks like you are already verified {0}.", HelperFunctions.GetRandomName(_random)));
            }
            else if (registeredUser.VerifyType != Consts.VERIFYTYPE_DISCORD)
            {
                await ReplyAsync(string.Format("Finish your web verification first, {0}.", HelperFunctions.GetRandomName(_random)));
            }
            else if (registeredUser.VerifyExpirationDateTime < DateTime.UtcNow && registeredUser.VerifyType == Consts.VERIFYTYPE_DISCORD)
            {
                await ReplyAsync(string.Format("Your verification expired. Run !ClearMe and the !RegisterMe again, and try to register with 30 minutes, {0}.", HelperFunctions.GetRandomName(_random)));
            }
            else
            {
                UplandProperty property = await _uplandApiRepository.GetPropertyById(registeredUser.PropertyId);
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
                    registeredUser.DiscordVerified = true;
                    registeredUser.VerifyType = null;
                    _localDataManager.UpdateRegisteredUser(registeredUser);

                    // Add the EOS Account if we dont have it
                    Tuple<string, string> currentUser = _localDataManager.GetUplandUsernameByEOSAccount(property.owner);
                    if (currentUser == null)
                    {
                        _localDataManager.UpsertEOSUser(property.owner, registeredUser.UplandUsername, DateTime.UtcNow);
                    }

                    await ReplyAsync(string.Format("You are now Verified {0}! You can remove the property from sale, or don't. I'm not your dad.", HelperFunctions.GetRandomName(_random)));
                }
            }
            return;
        }

        [Command("HowManyRuns")]
        public async Task HowManyRuns()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            if (registeredUser.Paid)
            {
                await ReplyAsync(string.Format("You're a Supporter {0}, you don't need to worry about runs anymore!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            await EnsureRunsAvailable(registeredUser);
        }

        [Command("OptimizerRun")]
        public async Task OptimizerRun()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            if (!await EnsureRunsAvailable(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.Id);
            if (currentRun != null && currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("You alread have a run in progress {0}. Try using my !OptimizerStatus command to track its progress.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (currentRun != null)
            {
                _localDataManager.DeleteOptimizerRuns(registeredUser.Id);
            }

            try
            {
                CollectionOptimizer optimizer = new CollectionOptimizer(_localDataManager, _uplandApiRepository);
                await ReplyAsync(string.Format("Got it {0}! I have started your optimization run.", HelperFunctions.GetRandomName(_random)));
                OptimizerRunRequest runRequest = new OptimizerRunRequest(registeredUser.UplandUsername.ToLower());
                await optimizer.RunAutoOptimization(registeredUser, runRequest);

                return;
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("Commands - OptimizerRun", ex.Message);
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }
        }

        [Command("OptimizerStatus")]
        public async Task OptimizerStatus()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.Id);
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
        public async Task OptimizerResults(string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.Id);
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
                OptimizerResults optimizerResults = JsonSerializer.Deserialize<OptimizerResults>(Encoding.UTF8.GetString(currentRun.Results));

                string optimizerString = "";

                if (fileType.ToUpper() == "TXT")
                {
                    optimizerString = string.Join(Environment.NewLine, CollectionOptimizer.BuildTextOutput(optimizerResults));
                }
                else
                {
                    optimizerString = string.Join(Environment.NewLine, CollectionOptimizer.BuildCsvOutput(optimizerResults));
                }

                byte[] resultBytes = Encoding.UTF8.GetBytes(optimizerString);
                using (Stream stream = new MemoryStream())
                {
                    stream.Write(resultBytes, 0, resultBytes.Length);
                    stream.Seek(0, SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(stream, string.Format("{0}_OptimizerResults.{1}", registeredUser.UplandUsername, fileType.ToUpper() == "TXT" ? "txt" : "csv"));
                }
            }
        }

        [Command("SupportMe")]
        public async Task SupportMe()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            if (registeredUser.Paid)
            {
                await ReplyAsync(string.Format("You are already a supporter {0}. Thanks for helping out!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            int upxToSupporter = Consts.SendUpxSupporterThreshold - registeredUser.SendUPX;

            List<string> supportMeString = new List<string>();

            supportMeString.Add(string.Format("Hey {0}, Sounds like you really like this tool! For the low price of ${1:N2} you will get perpetual access to run this when ever you like, access to additional features, and get a warm fuzzy feeling knowing you are helping to pay for hosting and development costs.", HelperFunctions.GetRandomName(_random), upxToSupporter / 1000.0));
            supportMeString.Add("");
            supportMeString.Add(string.Format("You can pay by sending at least ${0:N2} bucks to Grombrindal through the below methods. Always be sure to DM Grombrindal when you do!", upxToSupporter / 1000.0));
            supportMeString.Add(string.Format("   1. UPX - Offer {0} or more UPX on 9843 S Exchange Ave in Chicago, and DM Grombrindal with your upland username. I'll accept and buy it back for 1 UPX.", upxToSupporter));
            supportMeString.Add(string.Format("   2. UPX - Keep Sending to the properties in #locations. For each 200 UPX in sends to those locations you will earn another run. Once you have sent {0} UPX total, and the bot will automatically set you as a supporter", Consts.SendUpxSupporterThreshold));
            supportMeString.Add("   3. USD - Paypal - chri1852@umn.edu");
            supportMeString.Add("   4. USD - Venmo  - Alex-Christensen-9");
            supportMeString.Add("   5. WAX - Send to 5otpy.wam, with your upland username in the memo.");
            supportMeString.Add("   6. Crypto - Send it to Grombrindal via the Tipbot in the channel.");
            supportMeString.Add("   7. Anything Else - DM Grombindal and we'll work something out.");

            await ReplyAsync(string.Format("{0}", string.Join(Environment.NewLine, supportMeString)));
        }

        [Command("CollectionInfo")]
        public async Task CollectionInfo(string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            List<string> collectionData = _informationProcessor.GetCollectionInformation(fileType.ToUpper());

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, collectionData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("CollectionInfo.{0}", fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }
        }

        [Command("PropertyInfo")]
        public async Task PropertyInfo(string username = "___SELF___", string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            if (username == "___SELF___")
            {
                username = registeredUser.UplandUsername;
            }

            List<string> propertyData = await _informationProcessor.GetPropertyInfo(username.ToLower(), fileType.ToUpper());

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, propertyData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("PropertyInfo_{0}.{1}", username, fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }
        }

        [Command("NeighborhoodInfo")]
        public async Task NeighborhoodInfo(string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            List<string> neighborhoodData = _informationProcessor.GetNeighborhoodInformation(fileType.ToUpper());

            if (neighborhoodData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), neighborhoodData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, neighborhoodData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("NeighborhoodInfo.{0}", fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }
        }

        [Command("CollectionsForSale")]
        public async Task CollectionsForSale(int collectionId, string orderBy, string currency, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Sounds Good {0}! Lets find out whats for sale!", HelperFunctions.GetRandomName(_random)));
            List<string> collectionReport = _forSaleProcessor.GetCollectionPropertiesForSale(collectionId, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

            if (collectionReport.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), collectionReport[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, collectionReport));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("Collection_{0}_SalesData.{1}", collectionId, fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("NeighborhoodsForSale")]
        public async Task NeighborhoodsForSale(int neighborhoodId, string orderBy, string currency, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Sounds Good {0}! Lets find out whats for sale!", HelperFunctions.GetRandomName(_random)));
            List<string> neighborhoodReport = _forSaleProcessor.GetNeighborhoodPropertiesForSale(neighborhoodId, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

            if (neighborhoodReport.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), neighborhoodReport[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, neighborhoodReport));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("Neighborhood_{0}_SalesData.{1}", neighborhoodId, fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("CityInfo")]
        public async Task CityInfo(string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            List<string> cities = _informationProcessor.GetCityInformation(fileType.ToUpper());

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, cities));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("CityInfo.{0}", fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }
        }

        [Command("StreetInfo")]
        public async Task StreetInfo(string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            List<string> streets = _informationProcessor.GetStreetInformation(fileType.ToUpper());

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, streets));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("StreetInfo.{0}", fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }
        }

        [Command("CitysForSale")]
        public async Task CitysForSale(int cityId, string orderBy, string currency, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Running that query now {0}!", HelperFunctions.GetRandomName(_random)));
            List<string> salesData = _forSaleProcessor.GetCityPropertiesForSale(cityId, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

            if (salesData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), salesData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, salesData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("CitysForSaleData_{0}.{1}", cityId, fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("BuildingsForSale")]
        public async Task BuildingsForSale(string type, int Id, string orderBy, string currency, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Looking for Buildings now {0}!", HelperFunctions.GetRandomName(_random)));

            List<string> salesData = _forSaleProcessor.GetBuildingPropertiesForSale(type.ToUpper(), Id, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

            if (salesData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), salesData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, salesData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("BuildingsForSale.{0}", fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("StreetsForSale")]
        public async Task StreetsForSale(int streetId, string orderBy, string currency, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Running that query now {0}!", HelperFunctions.GetRandomName(_random)));
            List<string> salesData = _forSaleProcessor.GetStreetPropertiesForSale(streetId, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

            if (salesData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), salesData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, salesData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("StreetsForSaleData_{0}.{1}", streetId, fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("UsernameForSale")]
        public async Task UsernameForSale(string uplandUsername, string orderBy, string currency, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Running that query now {0}!", HelperFunctions.GetRandomName(_random)));
            List<string> salesData = _forSaleProcessor.GetUsernamePropertiesForSale(uplandUsername.ToLower(), orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

            if (salesData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), salesData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, salesData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("UserForSaleData_{0}.{1}", uplandUsername, fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("UnmintedProperties")]
        public async Task UnmintedProperties(string type, int Id, string propType, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Rodger that! Searching for unminted properties {0}!", HelperFunctions.GetRandomName(_random)));

            List<string> unmintedData = _informationProcessor.GetUnmintedProperties(type.ToUpper(), Id, propType.ToUpper(), fileType.ToUpper());

            if (unmintedData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), unmintedData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, unmintedData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("UnmintedProperties.{0}", fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("CatchWhales")]
        public async Task CatchWhales()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            List<string> whales = _informationProcessor.CatchWhales();
            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, whales));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("whales_{0}_{1}_{2}.csv", DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Year));
            }
        }

        [Command("AllProperties")]
        public async Task AllProperties(string type, int Id, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Okey-Dokey! Searching for all properties {0}!", HelperFunctions.GetRandomName(_random)));

            List<string> unmintedData = _informationProcessor.GetAllProperties(type.ToUpper(), Id, fileType.ToUpper());

            if (unmintedData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), unmintedData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, unmintedData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("AllProperties.{0}", fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("SearchStreets")]
        public async Task SearchStreets(string name, string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Sounds Good! Searching for streets {0}!", HelperFunctions.GetRandomName(_random)));

            List<string> streetsData = _informationProcessor.SearchStreets(name.ToUpper(), fileType.ToUpper());

            if (streetsData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), streetsData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, streetsData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("StreetSearchResults.{0}", fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("SearchNeighborhoods")]
        public async Task SearchNeighborhoods(string name, string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Sounds Great {0}! Searching for Neighborhoods!", HelperFunctions.GetRandomName(_random)));

            List<string> neighborhoodsData = _informationProcessor.SearchNeighborhoods(name.ToUpper(), fileType.ToUpper());

            if (neighborhoodsData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), neighborhoodsData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, neighborhoodsData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("NeighborhoodSearchResults.{0}", fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("SearchCollections")]
        public async Task SearchCollections(string name, string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Can Do {0}! Searching for Collections!", HelperFunctions.GetRandomName(_random)));

            List<string> collectionsData = _informationProcessor.SearchCollections(name.ToUpper(), fileType.ToUpper());

            if (collectionsData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), collectionsData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, collectionsData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("CollectionSearchResults.{0}", fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("SearchProperties")]
        public async Task SearchStreets(int cityId, string address, string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Sounds Good! Searching for Properties {0}!", HelperFunctions.GetRandomName(_random)));

            List<string> propertiesData = _informationProcessor.SearchProperties(cityId, address, fileType.ToUpper());

            if (propertiesData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), propertiesData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, propertiesData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("PropertySearchResults.{0}", fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("GetAssets")]
        public async Task GetAssets(string userName, string type, string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Sounds Good! I'll find those assets {0}!", HelperFunctions.GetRandomName(_random)));

            List<string> assetData = await _informationProcessor.GetAssetsByTypeAndUserName(type, userName, fileType.ToUpper());

            if (assetData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), assetData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, assetData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("{0}_Assets_{1}.{2}", type.ToUpper(), userName, fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("GetSalesHistory")]
        public async Task GetSalesHistory(string type, string identifier, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            await ReplyAsync(string.Format("Grabbing that Sales History now {0}!", HelperFunctions.GetRandomName(_random)));

            List<string> saleHistoryData = _informationProcessor.GetSaleHistoryByType(type.ToUpper(), identifier, fileType.ToUpper());

            if (saleHistoryData.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), saleHistoryData[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, saleHistoryData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("SaleHistory_{0}_{1}.{2}", type, identifier, fileType.ToUpper() == "CSV" ? "csv" : "txt"));
            }
        }

        [Command("Appraisal")]
        public async Task Appraisal(string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            AppraisalResults appraiserOutput = new AppraisalResults();

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            if (!await EnsureRunsAvailable(registeredUser))
            {
                return;
            }

            try
            {
                appraiserOutput = await _profileAppraiser.RunAppraisal(registeredUser);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("Commands - Appraisal", ex.Message);
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            string appraisalString = "";

            if (fileType.ToUpper() == "TXT")
            {
                appraisalString = string.Join(Environment.NewLine, _profileAppraiser.BuildAppraisalTxtStrings(appraiserOutput));
            } else
            {
                appraisalString = string.Join(Environment.NewLine, _profileAppraiser.BuildAppraisalCsvStrings(appraiserOutput));
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(appraisalString);
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("{0}_Appraisal.{1}", registeredUser.UplandUsername, fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }

            registeredUser.RunCount++;
            _localDataManager.UpdateRegisteredUser(registeredUser);
        }

        [Command("LastAppraisal")]
        public async Task LastAppraisal(string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            AppraisalRun currentRun = _localDataManager.GetLatestAppraisalRun(registeredUser.Id);
            if (currentRun == null)
            {
                await ReplyAsync(string.Format("I don't see any appraisals for you {0}. Try using my !Appraisal command to run one.", HelperFunctions.GetRandomName(_random)));
                return;
            }
            else
            {
                AppraisalResults results = JsonSerializer.Deserialize<AppraisalResults>(Encoding.UTF8.GetString(currentRun.Results));

                string appraisalString = "";

                if (fileType.ToUpper() == "TXT")
                {
                    appraisalString = string.Join(Environment.NewLine, _profileAppraiser.BuildAppraisalTxtStrings(results));
                }
                else
                {
                    appraisalString = string.Join(Environment.NewLine, _profileAppraiser.BuildAppraisalCsvStrings(results));
                }
 
                byte[] resultBytes = Encoding.UTF8.GetBytes(appraisalString);
                using (Stream stream = new MemoryStream())
                {
                    stream.Write(resultBytes, 0, resultBytes.Length);
                    stream.Seek(0, SeekOrigin.Begin);
                    await Context.Channel.SendFileAsync(stream, string.Format("{0}_Appraisal.{1}", registeredUser.UplandUsername, fileType.ToUpper() == "TXT" ? "txt" : "csv"));
                }
            }
        }

        [Command("CreateMap")]
        public async Task CreateMap(int cityId, string type, string colorBlind = "NOT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            string fileName = "";

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            if (!await EnsureRunsAvailable(registeredUser))
            {
                return;
            }

            CheckBlockChainStatus();

            if (!Consts.NON_BULLSHIT_CITY_IDS.Contains(cityId))
            {
                await ReplyAsync(string.Format("That City ID, {0} looks invalid, {1}!", cityId, HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (!_mappingProcessor.IsValidType(type))
            {
                await ReplyAsync(string.Format("{0} is not a valid map type {1}! Try any of {2}.",
                    type, 
                    HelperFunctions.GetRandomName(_random), 
                    string.Join(", ", _mappingProcessor.GetValidTypes())));

                return;
            }

            try
            {
                await ReplyAsync(string.Format("Creating the map now!"));
                fileName = _mappingProcessor.CreateMap(cityId, type, registeredUser.Id, colorBlind != "NOT", null);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("Commands - CreateMap - Build Map", ex.Message);
                await ReplyAsync(string.Format("Sorry, {0}. The Map blew up!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            await Context.Channel.SendFileAsync(_mappingProcessor.GetMapLocaiton(fileName));

            try
            {
                _mappingProcessor.DeleteSavedMap(fileName);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("Commands - CreateMap - Delete Map", ex.Message);
            }

            registeredUser.RunCount++;
            _localDataManager.UpdateRegisteredUser(registeredUser);
        }

        [Command("Help")]
        public async Task Help()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            List<UplandAuthProperty> properties;

            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("Looks like you don't exist {0}. To start try running !RegisterMe with your Upland username!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (registeredUser != null && registeredUser.DiscordUsername != null && registeredUser.DiscordUsername != "")
            {
                if (!registeredUser.DiscordVerified)
                {
                    properties = await _uplandApiRepository.GetPropertysByUsername(registeredUser.UplandUsername);
                    await ReplyAsync(string.Format("Looks like you have registered, but not verified yet {0}. The way I see it you have two choices.", registeredUser.UplandUsername));
                    await ReplyAsync(string.Format("1. Place the property at {0}, for sale for {1:N2}UPX, and then use my !VerifyMe command. Or...", properties.Where(p => p.Prop_Id == registeredUser.PropertyId).First().Full_Address, registeredUser.Price));
                    await ReplyAsync(string.Format("2. Run my !ClearMe command to clear your unverified registration, and register again with !RegisterMe."));
                    return;
                }
            }

            // They are registered now, display help
            if (registeredUser.Paid || registeredUser.SendUPX >= Consts.SendUpxSupporterThreshold)
            {
                await ReplyAsync(string.Format("Hey there {0}! Thanks for being a supporter!{1}{1}", HelperFunctions.GetRandomName(_random), Environment.NewLine));
            }
            else
            {
                int runsAvailable = Consts.FreeRuns + Convert.ToInt32(Math.Floor((double)(registeredUser.SendUPX / Consts.UPXPricePerRun)));
                await ReplyAsync(string.Format("Hello {0}! You have currenty used {1}/{2} of your runs. To get more vist the locations in #locations, or run my !SupportMe command.{3}{3}", HelperFunctions.GetRandomName(_random), registeredUser.RunCount, runsAvailable, Environment.NewLine));
            }

            List<string> helpMenu = new List<string>();

            helpMenu.Add("Below are the functions you can run use my !Help command and specify the number of the command you want more information on, like !Help 2.");
            helpMenu.Add("");
            helpMenu.Add("Run Limited Commands");
            helpMenu.Add("   1.  !OptimizerRun");
            helpMenu.Add("   2.  !Appraisal");
            helpMenu.Add("   3.  !CreateMap");
            helpMenu.Add("");
            helpMenu.Add("Free Commands");
            helpMenu.Add("   4.  !OptimizerStatus");
            helpMenu.Add("   5.  !OptimizerResults");
            helpMenu.Add("   6.  !LastAppraisal");
            helpMenu.Add("   7.  !CollectionInfo");
            helpMenu.Add("   8.  !PropertyInfo");
            helpMenu.Add("   9.  !NeighborhoodInfo");
            helpMenu.Add("   10. !CityInfo");
            helpMenu.Add("   11. !StreetInfo");
            helpMenu.Add("   12. !SupportMe");
            helpMenu.Add("   13. !CollectionsForSale");
            helpMenu.Add("   14. !NeighborhoodsForSale");
            helpMenu.Add("   15. !CitysForSale");
            helpMenu.Add("   16. !BuildingsForSale");
            helpMenu.Add("   17. !StreetsForSale");
            helpMenu.Add("   18. !UsernameForSale");
            helpMenu.Add("   19. !UnmintedProperties");
            helpMenu.Add("   20. !AllProperties");
            helpMenu.Add("   21. !SearchStreets");
            helpMenu.Add("   22. !SearchProperties");
            helpMenu.Add("   23. !SearchNeighborhoods");
            helpMenu.Add("   24. !SearchCollections");
            helpMenu.Add("   25. !GetAssets");
            helpMenu.Add("   26. !GetSalesHistory");
            helpMenu.Add("   27. !HowManyRuns");
            helpMenu.Add("");
            helpMenu.Add("Supporter Commands");
            helpMenu.Add("   28. !OptimizerLevelRun");
            helpMenu.Add("   28. !OptimizerWhatIfRun");
            helpMenu.Add("   30. !OptimizerExcludeRun");
            helpMenu.Add("");
            helpMenu.Add("Try the Website: https://www.uplandoptimizer.com/");
            helpMenu.Add("");
            await ReplyAsync(string.Format("{0}", string.Join(Environment.NewLine, helpMenu)));
        }

        [Command("Help")]
        public async Task Help(string command)
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            List<UplandAuthProperty> properties;

            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("Looks like you don't exist {0}. To start try running !RegisterMe with your Upland username!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (registeredUser != null && registeredUser.DiscordUsername != null && registeredUser.DiscordUsername != "")
            {
                if (!registeredUser.DiscordVerified)
                {
                    properties = await _uplandApiRepository.GetPropertysByUsername(registeredUser.UplandUsername);
                    await ReplyAsync(string.Format("Looks like you have registered, but not verified yet {0}. The way I see it you have two choices.", registeredUser.UplandUsername));
                    await ReplyAsync(string.Format("1. Place the property at {0}, for sale for {1:N2}UPX, and then use my !VerifyMe command. Or...", properties.Where(p => p.Prop_Id == registeredUser.PropertyId).First().Full_Address, registeredUser.Price));
                    await ReplyAsync(string.Format("2. Run my !ClearMe command to clear your unverified registration, and register again with !RegisterMe."));
                    return;
                }
            }

            List<string> helpOutput = HelperFunctions.GetHelpTextForCommandNumber(command);

            await ReplyAsync(string.Format("{0}", string.Join(Environment.NewLine, helpOutput)));
        }

        private async Task<bool> EnsureRegisteredAndVerified(RegisteredUser registeredUser)
        {
            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("You don't appear to exist {0}. Try again with my !RegisterMe command with your Upland username!", HelperFunctions.GetRandomName(_random)));
                return false;
            }

            if (!registeredUser.DiscordVerified)
            {
                await ReplyAsync(string.Format("Looks like you are not verified {0}. Try Again with my !VerifyMe command.", HelperFunctions.GetRandomName(_random)));
                return false;
            }

            return true;
        }

        private async Task CheckBlockChainStatus()
        {
            if (!bool.Parse(_localDataManager.GetConfigurationValue(Consts.CONFIG_ENABLEBLOCKCHAINUPDATES)))
            {
                await ReplyAsync(string.Format("Blockchain Updates are Disabled. This data is likely out of date."));
            }
        }

        public async Task<bool> EnsureRunsAvailable(RegisteredUser registeredUser)
        {
            if (!registeredUser.Paid && registeredUser.SendUPX >= Consts.SendUpxSupporterThreshold)
            {
                await (Context.User as IGuildUser).AddRoleAsync(Context.Guild.GetRole(Consts.DiscordSupporterRoleId));
                registeredUser.Paid = true;
                _localDataManager.UpdateRegisteredUser(registeredUser);
                await ReplyAsync(string.Format("Congrats and Thank You {0}! You have sent enough times to be considered a Supporter! Don't worry about runs anymore, you've done enough. You are no longer limited by runs, and have access to the Supporter Commands!", HelperFunctions.GetRandomName(_random)));
            }

            if (registeredUser.Paid || registeredUser.SendUPX >= Consts.SendUpxSupporterThreshold)
            {
                return true;
            }

            int runsAvailable = Consts.FreeRuns + Convert.ToInt32(Math.Floor((double)(registeredUser.SendUPX / Consts.UPXPricePerRun)));
            int upxToNextFreeRun = Consts.UPXPricePerRun - registeredUser.SendUPX % Consts.UPXPricePerRun;

            if (registeredUser.RunCount < runsAvailable)
            {
                await ReplyAsync(string.Format("You've used {0} out of {1} of your runs {2}. You are {3} UPX away from your next run, and {4} UPX from becoming a supporter. To put more UPX towards a run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", registeredUser.RunCount, runsAvailable, HelperFunctions.GetRandomName(_random), upxToNextFreeRun, Consts.SendUpxSupporterThreshold - registeredUser.SendUPX));
            }
            else if (registeredUser.RunCount >= runsAvailable)
            {
                await ReplyAsync(string.Format("You've used all of your runs {0}. You are {1} UPX away from your next run, and {2} UPX from becoming a supporter. To put more UPX towards a run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", HelperFunctions.GetRandomName(_random), upxToNextFreeRun, Consts.SendUpxSupporterThreshold - registeredUser.SendUPX));
                return false;
            }

            return true;
        }
    }
}
