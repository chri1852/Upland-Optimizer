using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
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
            switch(rand)
            {
                case 0:
                    await ReplyAsync(string.Format("Knock it off {0}!", GetRandomName()));
                    break;
                case 1:
                    await ReplyAsync(string.Format("Cool it {0}!", GetRandomName()));
                    break;
                case 2:
                    await ReplyAsync(string.Format("Put a sock in it {0}!", GetRandomName()));
                    break;
                case 3:
                    await ReplyAsync(string.Format("Quit it {0}!", GetRandomName()));
                    break;
                case 4:
                    await ReplyAsync(string.Format("Easy there {0}!", GetRandomName()));
                    break;
                case 5:
                    await ReplyAsync(string.Format("Dial it back {0}!", GetRandomName()));
                    break;
                case 6:
                    await ReplyAsync(string.Format("Cool your jets {0}!", GetRandomName()));
                    break;
                case 7:
                    await ReplyAsync(string.Format("Calm down a bit {0}!", GetRandomName()));
                    break;
            }
        }

        [Command("RegisterMe")]
        public async Task RegisterMe(string uplandUserName)
        {
            LocalDataManager localDataManager = new LocalDataManager();
            List<Property> properties;

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (registeredUser != null && registeredUser.DiscordUsername != null && registeredUser.DiscordUsername != "")
            {
                if (registeredUser.Verified)
                {
                    await ReplyAsync(string.Format("Looks like you already registered and verified {0}.", registeredUser.UplandUsername));
                }
                else
                {
                    properties = await localDataManager.GetPropertysByUsername(uplandUserName.ToLower());
                    await ReplyAsync(string.Format("Looks like you already registered {0}. The way I see it you have two choices.", registeredUser.UplandUsername));
                    await ReplyAsync(string.Format("1. Place the property at {0}, for sale for {1:C2}UPX, and then use my !VerifyMe command. Or...", properties.Where(p => p.Id == registeredUser.PropertyId).First().Address, registeredUser.Price));
                    await ReplyAsync(string.Format("2. Run my !ClearMe command to clear your unverified registration."));
                }
                return;
            }

            properties = await localDataManager.GetPropertysByUsername(uplandUserName.ToLower());
            if (properties.Count == 0)
            {
                await ReplyAsync(string.Format("Looks like {0} is not a player {1}.", uplandUserName, GetRandomName()));
                return;
            }

            Property verifyProperty = properties[_random.Next(0, properties.Count)];
            int verifyPrice = _random.Next(80000000, 90000000);

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
                localDataManager.CreateRegisteredUser(newUser);
            }
            catch
            {
                await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", GetRandomName()));
                return;
            }

            await ReplyAsync(string.Format("Good News {0}! I have registered you as a user!", GetRandomName()));
            await ReplyAsync(string.Format("To Continue place, {0}, up for sale for {1:C2} UPX, and then use my !VerifyMe command.", verifyProperty.Address, verifyPrice));
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
                    await ReplyAsync(string.Format("Looks like you are already verified {0}. Try contacting Grombrindal.", GetRandomName()));
                }
                else
                {
                    try
                    {
                        localDataManager.DeleteRegisteredUser(Context.User.Id);
                        await ReplyAsync(string.Format("I got you {0}. I have cleared your registration. Try again with my !RegisterMe command!", GetRandomName()));
                    }
                    catch
                    {
                        await ReplyAsync(string.Format("Sorry, {0}. Looks like I goofed!", GetRandomName()));
                        return;
                    }
                }
                return;
            }

            await ReplyAsync(string.Format("You don't appear to exist {0}. Try again with my !RegisterMe command!", GetRandomName()));
        }

        [Command("VerifyMe")]
        public async Task VerifyMe()
        {
            LocalDataManager localDataManager = new LocalDataManager();
            UplandApiRepository uplandApiRepository = new UplandApiRepository();

            RegisteredUser registeredUser = localDataManager.GetRegisteredUser(Context.User.Id);
            if (registeredUser == null || registeredUser.DiscordUsername == null || registeredUser.DiscordUsername == "")
            {
                await ReplyAsync(string.Format("You don't appear to exist {0}. Try again with my !RegisterMe command!", GetRandomName()));
            }

            if (registeredUser.Verified)
            {
                await ReplyAsync(string.Format("Looks like you are already verified {0}.", GetRandomName()));
            }
            else
            {
                UplandDistinctProperty property = await uplandApiRepository.GetPropertyById(registeredUser.PropertyId);
                if (property.on_market == null)
                {
                    await ReplyAsync(string.Format("Doesn't look like {0} is on sale {1}", property.Full_Address, GetRandomName()));
                    return;
                }

                if (property.on_market.token != string.Format("{0}.00 UPX", registeredUser.Price))
                {
                    await ReplyAsync(string.Format("{0} is on sale, but it not for {1:C2}", property.Full_Address, registeredUser.Price));
                    return;
                }
                else
                {
                    localDataManager.SetRegisteredUserVerified(registeredUser.UplandUsername);
                    await ReplyAsync(string.Format("You are now Verified {0}! You can remove the property from sale, or don't. I'm not your dad.", GetRandomName()));
                }
            }
            return; 
        }

        private string GetRandomName()
        {
            List<string> names = new List<string>
            {
                "Friendo",
                "Chief",
                "Slugger",
                "Boss",
                "Champ",
                "Amigo",
                "Guy",
                "Buddy",
                "Sport",
                "My Dude",
                "Pal",
                "Buddy",
                "Bud",
                "Big Guy",
                "Tiger",
                "Scooter",
                "Shooter",
                "Ace",
                "Partner",
                "Slick",
                "Hombre",
                "Hoss",
                "Bub",
                "Buster",
                "Partner"
            };

            return names[_random.Next(names.Count)];
        }
    }
}
