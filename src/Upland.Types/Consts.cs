using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types
{
    public static class Consts
    {
        // Windows DB
        //public static readonly string LocalDBConnectionString = @"Data Source=DESKTOP-BK6KAFH\SQLEXPRESS;Initial Catalog=UplandLocalData;Integrated Security=True;";
        //public static readonly string LocalDBConnectionString = @"Data Source=localhost;Initial Catalog=UplandLocalData;User Id= SA;Password=G0dDamnInternet;";

        // Config Values
        public static readonly string CONFIG_MAXGLOBALSEQUENCE = "MaxGlobalSequence";

        public static readonly string CityPro = "City Pro";
        public static readonly string KingOfTheStreet = "King of the Street";
        public static readonly string Newbie = "Newbie";

        public static readonly double NewbieBoost = 1.1;

        public static readonly int CityProId = 21;
        public static readonly int KingOfTheStreetId = 1;
        public static readonly int NewbieId = 7;

        public static readonly List<int> StandardCollectionIds = new List<int>
        {
            1,   // King of the Street
            7,   // Newbie
            21,  // City Pro
        };

        public static readonly Dictionary<int, string> Cities = new Dictionary<int, string>
        {
            { 1    , "San Francisco" },
            { 3    , "Manhattan"     },
            { 4    , "Queens"        },
            { 5    , "Fresno"        },
            { 6    , "Brooklyn"      },
            { 7    , "Oakland"       },
            { 8    , "Staten Island" },
            { 9    , "Bakersfield"   },
            { 10   , "Chicago"       },
            { 11   , "Cleveland"     },
            { 12   , "Santa Clara"   },
            { 13   , "Rutherford"    },
            { 14   , "Kansas"        },
            { 15   , "New Orleans"   },
            { 16   , "Nashville"     },
            { 10000, "New York"      },
            { 10001, "Clovis"        },
            { 10002, "Piedmont"      },
        };

        public static readonly int WarningRuns = 1;
        public static readonly int FreeRuns = 6;
        public static readonly string RunStatusInProgress = "In Progress";
        public static readonly string RunStatusCompleted= "Completed";
        public static readonly string RunStatusFailed = "Failed";
        public static readonly ulong TestUserDiscordId = 1;

        public static readonly int MaxStreetNumber = 38807;
    }
}
