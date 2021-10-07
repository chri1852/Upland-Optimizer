using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
