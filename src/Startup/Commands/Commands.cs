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

        public Commands(InformationProcessor informationProcessor)
        {
            _random = new Random();
            _informationProcessor = informationProcessor;
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
                CollectionOptimizer optimizer = new CollectionOptimizer();
                await ReplyAsync(string.Format("Got it {0}! I have started your optimization run.", HelperFunctions.GetRandomName(_random)));
                await optimizer.RunAutoOptimization(registeredUser, 7);

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
                await ReplyAsync(string.Format("For the low price of $5 you will get perpetual access to run this when ever you like, access to additional features, and get a warm fuzzy feeling knowing you are helping to pay for hosting and development costs. USD, UPX, Waxp, Ham Sandwiches, MTG Bulk Rares, and more are all accepted in payment."));
            }
        }

        [Command("CollectionInfo")]
        public async Task CollectionInfo(string fileType = "TXT")
        {
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            List<string> collectionData = _informationProcessor.GetCollectionInformation(fileType.ToUpper());

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, collectionData));
            using(Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("CollectionInfo.{0}", fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }
        }

        [Command("PropertyInfo")]
        public async Task PropertyInfo(string fileType = "TXT")
        {
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            List<string> propertyData = await _informationProcessor.GetPropertyInfo(registeredUser.UplandUsername, fileType.ToUpper());

            byte[] resultBytes = Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, propertyData));
            using (Stream stream = new MemoryStream())
            {
                stream.Write(resultBytes, 0, resultBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                await Context.Channel.SendFileAsync(stream, string.Format("PropertyInfo_{0}.{1}", registeredUser.UplandUsername, fileType.ToUpper() == "TXT" ? "txt" : "csv"));
            }
        }

        [Command("NeighborhoodInfo")]
        public async Task NeighborhoodInfo(string fileType = "TXT")
        {
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Sounds Good {0}! Lets find out whats for sale!", HelperFunctions.GetRandomName(_random)));
            List<string> collectionReport = await _informationProcessor.GetCollectionPropertiesForSale(collectionId, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

            if(collectionReport.Count == 1)
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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Sounds Good {0}! Lets find out whats for sale!", HelperFunctions.GetRandomName(_random)));
            List<string> neighborhoodReport = await _informationProcessor.GetNeighborhoodPropertiesForSale(neighborhoodId, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Running that query now {0}!", HelperFunctions.GetRandomName(_random)));
            List<string> salesData = await _informationProcessor.GetCityPropertiesForSale(cityId, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Looking for Buildings now {0}!", HelperFunctions.GetRandomName(_random)));

            List<string> salesData = await _informationProcessor.GetBuildingPropertiesForSale(type.ToUpper(), Id, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

            if (!await EnsureRegisteredAndVerified(registeredUser))
            {
                return;
            }

            await ReplyAsync(string.Format("Running that query now {0}!", HelperFunctions.GetRandomName(_random)));
            List<string> salesData = await _informationProcessor.GetStreetPropertiesForSale(streetId, orderBy.ToUpper(), currency.ToUpper(), fileType.ToUpper());

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

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
            LocalDataManager localDataManager = new LocalDataManager();
            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);

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
            helpMenu.Add("");
            helpMenu.Add("Supporter Commands");
            helpMenu.Add("   18. !OptimizerLevelRun");
            helpMenu.Add("   19. !OptimizerWhatIfRun");
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
                    helpOutput.Add(string.Format("This command will start an optimizer run for you. Standard users get 6 free runs, while supporters can run this as many times as they like. To get your results or check on the status, run the !OptimizerResults or !OptimizerStatus commands. The first time your run the optimizer it may take some extra time as the system retrieves your property information from Upland."));
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
                    helpOutput.Add(string.Format("This command will return a text file with information on all collections, as well as the most recent mint percent, and property status counts. Note that the property count does not include any locked properties, city and standard collections will also have a property count of 0."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionInfo");
                    helpOutput.Add("This command will return a text file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "5":
                    helpOutput.Add(string.Format("!PropertyInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a csv file with all of your properties."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !PropertyInfo");
                    helpOutput.Add("This command will return a text file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !PropertyInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "6":
                    helpOutput.Add(string.Format("!NeighborhoodInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with information on all Neighborhoods, as well as the most recent mint percent, and property status counts."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodInfo");
                    helpOutput.Add("This command will return a text file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "7":
                    helpOutput.Add(string.Format("!CityInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with all cityIds and Names, as well as the most recent mint percent, and property status counts."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CityInfo");
                    helpOutput.Add("This command will return a text file");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CityInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "8":
                    helpOutput.Add(string.Format("!StreetInfo"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will return a text file with all streetIds and Names, as well as the most recent mint percent, and property status counts. Note you probably want this one to return as a CSV as there is a lot of street data."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetInfo");
                    helpOutput.Add("This command will return a text file");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetInfo CSV");
                    helpOutput.Add("This command will return a csv file.");
                    break;
                case "9":
                    helpOutput.Add(string.Format("!SupportMe"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will let you know how to support the development of this tool."));
                    break;
                case "10":
                    helpOutput.Add(string.Format("!CollectionsForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props for sale in the given collection id (not standard or city collections), and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. Note the sales data is cached to prevent you motley fools from accidently pinging upland to death. The oldest the data can get is 15 minutes before it is expired and fetched again. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionsForSale 177 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties for sale for UPX in the Kansas City Main St collection, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionsForSale 177 PRICE ALL");
                    helpOutput.Add("The above command finds all properties for sale in the Kansas City Main St collection, and returns a csv file from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CollectionsForSale 177 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties for sale for USD in the Kansas City Main St collection, and returns a txt file from from lowest to greatest price.");
                    break;
                case "11":
                    helpOutput.Add(string.Format("!NeighborhoodsForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props for sale in the given neighborhood id, and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. Note the sales data is cached to prevent you motley fools from accidently pinging upland to death. The oldest the data can get is 15 minutes before it is expired and fetched again. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodsForSale 810 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties for sale for UPX in the Chicago Ashburn neighborhood, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodsForSale 810 PRICE ALL");
                    helpOutput.Add("The above command finds all properties for sale in the Chicago Ashburn neighborhood, and returns a csv from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !NeighborhoodsForSale 810 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties for sale for USD in the Chicago Ashburn neighborhood, and returns a txt file from lowest to greatest price.");
                    break;
                case "12":
                    helpOutput.Add(string.Format("!CitysForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props for sale in the given city id, and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. Note the sales data is cached to prevent you motley fools from accidently pinging upland to death. The oldest the data can get is 15 minutes before it is expired and fetched again. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CitysForSale 10 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties for sale for UPX in the Chicago, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CitysForSale 1 PRICE ALL");
                    helpOutput.Add("The above command finds all properties for sale in the San Francisco, and returns a csv from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !CitysForSale 10 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties for sale for USD in the Chicago, and returns a txt file from lowest to greatest price.");
                    break;
                case "13":
                    helpOutput.Add(string.Format("!BuildingsForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props with Buildings for sale in the given type and id, and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. Note the sales data is cached to prevent you motley fools from accidently pinging upland to death. The oldest the data can get is 15 minutes before it is expired and fetched again. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !BuildingsForSale City 1 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties with buildings for sale for UPX in San Francisco, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !BuildingsForSale Street 31898 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties with buildings for sale on Broadway in Nashville for UPX, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !BuildingsForSale Neighborhood 876 PRICE ALL");
                    helpOutput.Add("The above command finds all properties with buildings for sale in the Chicago Portage Park neighborhood, and returns a csv file from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !BuildingsForSale Collection 2 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties with buildings for sale for USD in the Mission District Collection, and returns a txt file from lowest to greatest price.");
                    break;
                case "14":
                    helpOutput.Add(string.Format("!StreetsForSale"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find props for sale on the given street id, and return a csv file listing in order of MARKUP or PRICE, for sales in ALL currencys, USD, or UPX. Note the sales data is cached to prevent you motley fools from accidently pinging upland to death. The oldest the data can get is 15 minutes before it is expired and fetched again. If you want a text file add TXT to the end of the command."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetsForSale 31898 MARKUP UPX");
                    helpOutput.Add("The above command finds all properties for sale for UPX on Broadway in Nashville, and returns a csv file from lowest to greatest markup.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetsForSale 31898 PRICE ALL");
                    helpOutput.Add("The above command finds all properties for sale on Broadway in Nashville, and returns a csv from lowest to greatest price (USD = UPX * 1000).");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !StreetsForSale 28029 PRICE USD TXT");
                    helpOutput.Add("The above command finds all properties for sale for USD on Main St in Kansas City, and returns a txt file from lowest to greatest price.");
                    break;
                case "15":
                    helpOutput.Add(string.Format("!UnmintedProperties"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find unminted properties in the given type and id, and return a csv file listing in order of mint price. Depending on the city this data may be old. Cities that have not sold out are updated every night. Sold out cities are updated Saturday night."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !UnmintedProperties City 1 FSA");
                    helpOutput.Add("The above command finds all FSA unminted properties in San Francisco, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !UnmintedProperties Street 31898 NONFSA");
                    helpOutput.Add("The above command finds all non-FSA unminted properties on Broadway in Nashville, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !UnmintedProperties Neighborhood 876 ALL");
                    helpOutput.Add("The above command finds all unminted properties in the Chicago Portage Park neighborhood, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !UnmintedProperties Collection 2 ALL TXT");
                    helpOutput.Add("The above command finds all unminted properties in the Mission District Collection, and returns a txt file from lowest to greatest mint price.");
                    break;
                case "16":
                    helpOutput.Add(string.Format("!AllProperties"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will find all properties in the given type and id, and return a csv file listing in order of mint price. On the city level this will only work for Rutherford and Santa Clara."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !AllProperties City 12");
                    helpOutput.Add("The above command finds all FSA unminted properties in Santa Clara, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !AllProperties Street 31898");
                    helpOutput.Add("The above command finds all FSA unminted properties on Broadway in Nashville, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !AllProperties Neighborhood 876");
                    helpOutput.Add("The above command finds all properties in the Chicago Portage Park neighborhood, and returns a csv file from lowest to greatest mint price.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !AllProperties Collection 2 TXT");
                    helpOutput.Add("The above command finds all properties in the Mission District Collection, and returns a txt file from lowest to greatest mint price.");
                    break;
                case "17":
                    helpOutput.Add(string.Format("!SearchStreets"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command search for streets with the given name, and return a txt file with the matching street names."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchStreets Main");
                    helpOutput.Add("The above command finds all streets with MAIN in their name and returns a txt file.");
                    helpOutput.Add("");
                    helpOutput.Add("EX: !SearchStreets Broadway csv");
                    helpOutput.Add("The above command finds all streets with BROADWAY in their name, and returns a csv file.");
                    break;
                case "18":
                    helpOutput.Add(string.Format("!OptimizerLevelRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will run an optimizer run with a level you specify between 3 and 10. Levels 9 and especially 10 can take quite some time to run. You can get the results and check the status with the standard !OptimizerStatus and !OptimizerResults commands."));
                    helpOutput.Add("");
                    helpOutput.Add("EX: !OptimizerLevelRun 5");
                    break;
                case "19":
                    helpOutput.Add(string.Format("!OptimizerWhatIfRun"));
                    helpOutput.Add("");
                    helpOutput.Add(string.Format("This command will run an optimizer run with some additional fake properties in the requested collection. You will need to specify the collection Id to add the properties to, the number of properties to add, and the average monthly upx of the properties. You can get the results and check the status with the standard !OptimizerStatus and !OptimizerResults commands."));
                    helpOutput.Add("");
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
