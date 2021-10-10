using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Startup
{
    public static class HelperFunctions
    {
        public static string GetRandomName(Random random)
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
                "Partner",
                "Fam"
            };

            return names[random.Next(names.Count)];
        }

        public static List<string> BreakLongMessage(string message)
        {
            List<string> results = message.Split(Environment.NewLine, StringSplitOptions.None).ToList();
            List<string> stringGroups = new List<string>();
            string splitMessage = "";

            foreach (string entry in results)
            {
                if (splitMessage.Length + entry.Length + 1 < 2000)
                {
                    splitMessage += entry;
                    splitMessage += Environment.NewLine;
                }
                else
                {
                    stringGroups.Add(splitMessage);
                    splitMessage = entry;
                    splitMessage += Environment.NewLine;
                }
            }

            stringGroups.Add(splitMessage);

            return stringGroups;
        }
    }
}
