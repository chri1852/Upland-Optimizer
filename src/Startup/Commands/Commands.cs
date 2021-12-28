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
        private readonly InformationProcessor _informationProcessor;
        private readonly LocalDataManager _localDataManager;
        private readonly ProfileAppraiser _profileAppraiser;
        private readonly ForSaleProcessor _forSaleProcessor;

        public Commands(InformationProcessor informationProcessor, LocalDataManager localDataManager, ProfileAppraiser profileAppraiser, ForSaleProcessor forSaleProcessor)
        {
            _random = new Random();
            _informationProcessor = informationProcessor;
            _localDataManager = localDataManager;
            _profileAppraiser = profileAppraiser;
            _forSaleProcessor = forSaleProcessor;
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
            UplandApiRepository uplandApiRepository = new UplandApiRepository();
            List<UplandAuthProperty> properties;

            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
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

            RegisteredUser newUser = new RegisteredUser()
            {
                DiscordUserId = Context.User.Id,
                DiscordUsername = Context.User.Username,
                UplandUsername = uplandUserName.ToLower(),
                PropertyId = verifyProperty.Id,
                Price = verifyPrice
            };

            try
            {
                _localDataManager.CreateRegisteredUser(newUser);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("Commands - RegisterMe", ex.Message);
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            await ReplyAsync(string.Format("Good News {0}! I have registered you as a user!", HelperFunctions.GetRandomName(_random)));
            await ReplyAsync(string.Format("To continue place, {0}, {1}, up for sale for {2:N2} UPX, and then use my !VerifyMe command. If you can't place the propery for sale run my !ClearMe command.", verifyProperty.Address, Consts.Cities[verifyProperty.CityId], verifyPrice));
        }

        [Command("ClearMe")]
        public async Task ClearMe()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
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
                        _localDataManager.DeleteRegisteredUser(Context.User.Id);
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
            UplandApiRepository uplandApiRepository = new UplandApiRepository();

            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
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
                    _localDataManager.SetRegisteredUserVerified(registeredUser.DiscordUserId);

                    // Add the EOS Account if we dont have it
                    Tuple<string, string> currentUser = _localDataManager.GetUplandUsernameByEOSAccount(property.owner);
                    if (currentUser == null)
                    {
                        _localDataManager.UpsertEOSUser(property.owner, registeredUser.UplandUsername, DateTime.UtcNow);
                    }
                    else
                    {
                        _localDataManager.CreateErrorLog("Commands - VerifyMe", string.Format("Could Not Find EOS User with Username {0}", registeredUser.UplandUsername));
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

            int freeRuns = Consts.FreeRuns + Convert.ToInt32(Math.Floor((double)(registeredUser.SentUPX / Consts.UPXPricePerRun)));
            int upxToNextFreeRun = Consts.UPXPricePerRun - registeredUser.SentUPX % Consts.UPXPricePerRun;

            if (registeredUser.RunCount > Consts.WarningRuns && registeredUser.RunCount < freeRuns)
            {
                await ReplyAsync(string.Format("You've used {0} out of {1} of your runs {2}. You are {3} upx away from your next free run. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", registeredUser.RunCount, freeRuns, HelperFunctions.GetRandomName(_random), upxToNextFreeRun));
                return;
            }
            else if (registeredUser.RunCount >= freeRuns)
            {

                await ReplyAsync(string.Format("You've used all of your runs {0}. You are {1} upx away from your next free run. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", HelperFunctions.GetRandomName(_random), upxToNextFreeRun));

                return;
            }
        }

        [Command("OptimizerRun")]
        public async Task OptimizerRun()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            int freeRuns = Consts.FreeRuns + Convert.ToInt32(Math.Floor((double)(registeredUser.SentUPX / Consts.UPXPricePerRun)));
            int upxToNextFreeRun = Consts.UPXPricePerRun - registeredUser.SentUPX % Consts.UPXPricePerRun;

            if (!registeredUser.Paid && registeredUser.RunCount > Consts.WarningRuns && registeredUser.RunCount < freeRuns)
            {
                if (upxToNextFreeRun != 0)
                {
                    await ReplyAsync(string.Format("You've used {0} out of {1} of your runs {2}. You are {3} upx away from your next free run. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", registeredUser.RunCount, freeRuns, HelperFunctions.GetRandomName(_random), upxToNextFreeRun));
                }
                else
                {
                    await ReplyAsync(string.Format("You've used {0} out of {1} of your runs {2}. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", registeredUser.RunCount, freeRuns, HelperFunctions.GetRandomName(_random)));
                }
            }
            else if (!registeredUser.Paid && registeredUser.RunCount >= freeRuns)
            {
                if (upxToNextFreeRun != 0)
                {
                    await ReplyAsync(string.Format("You've used all of your runs {0}. You are {1} upx away from your next free run. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", HelperFunctions.GetRandomName(_random), upxToNextFreeRun));
                }
                else
                {
                    await ReplyAsync(string.Format("You've used all of your runs {0}. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", HelperFunctions.GetRandomName(_random)));
                }
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);
            if (currentRun != null && currentRun.Status == Consts.RunStatusInProgress)
            {
                await ReplyAsync(string.Format("You alread have a run in progress {0}. Try using my !OptimizerStatus command to track its progress.", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (currentRun != null)
            {
                _localDataManager.DeleteOptimizerRuns(registeredUser.DiscordUserId);
            }

            try
            {
                CollectionOptimizer optimizer = new CollectionOptimizer();
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

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);
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
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            OptimizationRun currentRun = _localDataManager.GetLatestOptimizationRun(registeredUser.DiscordUserId);
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
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

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
                int freeRuns = Consts.FreeRuns + Convert.ToInt32(Math.Floor((double)(registeredUser.SentUPX / Consts.UPXPricePerRun)));

                if (registeredUser.RunCount >= freeRuns - 2)
                {
                    string response = "Looks like you are ";
                    if (registeredUser.RunCount >= freeRuns)
                    {
                        response += "all ";
                    }
                    else
                    {
                        response += "almost all ";
                    }
                    await ReplyAsync(string.Format("{0}out of Optimizer Runs. You can earn new runs by sending the properties listed in the Locations channel. Once you send 500 UPX worth you will earn an additional Optimizer Run, or you can become a support to get Unlimited Optimizer Runs.", response));
                }

                await ReplyAsync(string.Format("Hey {0}, Sounds like you really like this tool, to help support this tool why don't you ping Grombrindal.{1}{1}", HelperFunctions.GetRandomName(_random), Environment.NewLine));
                await ReplyAsync(string.Format("For the low price of $5 you will get perpetual access to run this when ever you like, access to additional features, and get a warm fuzzy feeling knowing you are helping to pay for hosting and development costs. USD, UPX, Waxp, Ham Sandwiches, MTG Bulk Rares, and more are all accepted in payment."));
            }
        }

        [Command("CollectionInfo")]
        public async Task CollectionInfo(string fileType = "TXT")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

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

        [Command("UnmintedProperties")]
        public async Task UnmintedProperties(string type, int Id, string propType, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

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

        [Command("AllProperties")]
        public async Task AllProperties(string type, int Id, string fileType = "CSV")
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

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
            List<string> appraiserOutput = new List<string>();

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            int freeRuns = Consts.FreeRuns + Convert.ToInt32(Math.Floor((double)(registeredUser.SentUPX / Consts.UPXPricePerRun)));
            int upxToNextFreeRun = Consts.UPXPricePerRun - registeredUser.SentUPX % Consts.UPXPricePerRun;

            if (!registeredUser.Paid && registeredUser.RunCount > Consts.WarningRuns && registeredUser.RunCount < freeRuns)
            {
                if (upxToNextFreeRun != 0)
                {
                    await ReplyAsync(string.Format("You've used {0} out of {1} of your runs {2}. You are {3} upx away from your next free run. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", registeredUser.RunCount, freeRuns, HelperFunctions.GetRandomName(_random), upxToNextFreeRun));
                }
                else
                {
                    await ReplyAsync(string.Format("You've used {0} out of {1} of your runs {2}. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", registeredUser.RunCount, freeRuns, HelperFunctions.GetRandomName(_random)));
                }
            }
            else if (!registeredUser.Paid && registeredUser.RunCount >= freeRuns)
            {
                if (upxToNextFreeRun != 0)
                {
                    await ReplyAsync(string.Format("You've used all of your runs {0}. You are {1} upx away from your next free run. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", HelperFunctions.GetRandomName(_random), upxToNextFreeRun));
                }
                else
                {
                    await ReplyAsync(string.Format("You've used all of your runs {0}. To put more UPX towards a free run visit the properties list in the locations channel. To learn how to support this tool try my !SupportMe command.", HelperFunctions.GetRandomName(_random)));
                }
                return;
            }

            try
            {
                appraiserOutput = await _profileAppraiser.RunAppraisal(registeredUser.UplandUsername.ToLower(), fileType);
            }
            catch (Exception ex)
            {
                _localDataManager.CreateErrorLog("Commands - Appraisal", ex.Message);
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", HelperFunctions.GetRandomName(_random)));
                return;
            }

            if (appraiserOutput.Count == 1)
            {
                // An Error Occured
                await ReplyAsync(string.Format("Sorry {0}! {1}", HelperFunctions.GetRandomName(_random), appraiserOutput[0]));
                return;
            }

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, appraiserOutput));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("{0}_Appraisal.{1}", registeredUser.UplandUsername, fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }

            _localDataManager.IncreaseRegisteredUserRunCount(registeredUser.DiscordUserId);
        }

        [Command("Help")]
        public async Task Help()
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
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
            helpMenu.Add("   1.  !OptimizerRun");
            helpMenu.Add("   2.  !OptimizerStatus");
            helpMenu.Add("   3.  !OptimizerResults");
            helpMenu.Add("   4.  !CollectionInfo");
            helpMenu.Add("   5.  !PropertyInfo");
            helpMenu.Add("   6.  !NeighborhoodInfo");
            helpMenu.Add("   7.  !CityInfo");
            helpMenu.Add("   8.  !StreetInfo");
            helpMenu.Add("   9.  !SupportMe");
            helpMenu.Add("   10. !CollectionsForSale");
            helpMenu.Add("   11. !NeighborhoodsForSale");
            helpMenu.Add("   12. !CitysForSale");
            helpMenu.Add("   13. !BuildingsForSale");
            helpMenu.Add("   14. !StreetsForSale");
            helpMenu.Add("   15. !UnmintedProperties");
            helpMenu.Add("   16. !AllProperties");
            helpMenu.Add("   17. !SearchStreets");
            helpMenu.Add("   18. !SearchProperties");
            helpMenu.Add("   19. !GetAssets");
            helpMenu.Add("   20. !GetSalesHistory");
            helpMenu.Add("   21. !Appraisal");
            helpMenu.Add("   22. !HowManyRuns");
            helpMenu.Add("");
            helpMenu.Add("Supporter Commands");
            helpMenu.Add("   23. !OptimizerLevelRun");
            helpMenu.Add("   24. !OptimizerWhatIfRun");
            helpMenu.Add("   25. !OptimizerExcludeRun");
            helpMenu.Add("");
            await ReplyAsync(string.Format("{0}", string.Join(Environment.NewLine, helpMenu)));
        }

        [Command("Help")]
        public async Task Help(string command)
        {
            RegisteredUser registeredUser = _localDataManager.GetRegisteredUser(Context.User.Id);
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

            if (!registeredUser.Verified)
            {
                await ReplyAsync(string.Format("Looks like you are not verified {0}. Try Again with my !VerifyMe command.", HelperFunctions.GetRandomName(_random)));
                return false;
            }

            return true;
        }
    }
}
