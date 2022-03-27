using System.Collections.Generic;

namespace Upland.Types
{
    public static class Consts
    {
        // Config Values
        public const string CONFIG_ENABLEBLOCKCHAINUPDATES = "EnableBlockChainUpdates";
        public const string CONFIG_PROPIDSTOMONITORFORSENDS = "PropIdsToMonitorForSends";
        public const string CONFIG_MAXUPLANDACTIONSEQNUM = "MaxUplandActionSeqNum";
        public const string CONFIG_MAXUSPKTOKENACCACTIONSEQNUM = "MaxUSPKTokenAccActionSeqNum";
        public const string CONFIG_MAXUPLANDNFTACTACTIONSEQNUM = "MaxUplandNFTActActionSeqNum";
        public const string CONFIG_LATESTANNOUNCEMENT = "LatestAnnouncement";

        // NFT Metadata Types
        public const string METADATA_TYPE_STRUCTURE = "structure";
        public const string METADATA_TYPE_STRUCTORNMT = "structornmt";
        public const string METADATA_TYPE_SPIRITHLWN = "spirithlwn";
        public const string METADATA_TYPE_MEMENTO = "memento";
        public const string METADATA_TYPE_ESSENTIAL = "essential";
        public const string METADATA_TYPE_BLKEXPLORER = "blkexplorer";

        public static readonly List<int> StandardCollectionIds = new List<int>
        {
            1,   // King of the Street
            7,   // Newbie
            21,  // City Pro
        };

        // Property Status
        public const string PROP_STATUS_LOCKED = "Locked";
        public const string PROP_STATUS_OWNED = "Owned";
        public const string PROP_STATUS_UNLOCKED = "Unlocked";
        public const string PROP_STATUS_FORSALE = "For sale";

        public static readonly List<int> NON_BULLSHIT_CITY_IDS = new List<int>
        {
            1, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 29, 32, 33
        };

        public static readonly Dictionary<int, string> Cities = new Dictionary<int, string>
        {
            // Main Cities
            { 1    , "San Francisco"  },
            { 3    , "Manhattan"      },
            { 4    , "Queens"         },
            { 5    , "Fresno"         },
            { 6    , "Brooklyn"       },
            { 7    , "Oakland"        },
            { 8    , "Staten Island"  },
            { 9    , "Bakersfield"    },
            { 10   , "Chicago"        },
            { 11   , "Cleveland"      },
            { 12   , "Santa Clara"    },
            { 13   , "Rutherford"     },
            { 14   , "Kansas City"    },
            { 15   , "New Orleans"    },
            { 16   , "Nashville"      },
            { 29   , "Bronx"          },
            { 32   , "Los Angeles"    },
            { 33   , "Detroit"        },

            // NYC Subcities
            { 17   , "New York"       },

            // Fresno Subcities
            { 18   , "Clovis"         },

            // Oakland Subcities
            { 19   , "Piedmont"       },
            { 30   , "Alameda"        },
            { 31   , "Berkeley"       },

            // Nashville Subcities
            { 20   , "Joelton"        },
            { 21   , "Goodlettsville" },
            { 22   , "Ashland City"   },
            { 23   , "Madison"        },
            { 24   , "Old Hickory"    },
            { 25   , "Hermitage"      },
            { 26   , "Antioch"        },
            { 27   , "Nolensville"    },
            { 28   , "Whites Creek"   },

            // Los Angeles Subcities
            { -1   , "Inglewood"      },
        };

        public static readonly Dictionary<int, string> CollectionCategories = new Dictionary<int, string>
        {
            { 1 , "Standard"   },
            { 2 , "Limited"    },
            { 3 , "Exclusive"  },
            { 4 , "Rare"       },
            { 5 , "Ultra Rare" }
        };

        public const int WarningRuns = 1;
        public const int FreeRuns = 6;
        public const string RunStatusInProgress = "In Progress";
        public const string RunStatusCompleted = "Completed";
        public const string RunStatusFailed = "Failed";
        public const int TestUserId = 33;
        public const int UPXPricePerRun = 200;
        public const int SendUpxSupporterThreshold = 8000;
        public const ulong DiscordSupporterRoleId = 910751643857997824;
        public const string VERIFYTYPE_DISCORD = "DIS";
        public const string VERIFYTYPE_WEB = "WEB";
        public const string VERIFYTYPE_RESET = "WRS";

        public const int MAX_LINES_TO_RETURN = 25000;
        //public const double RateOfReturn = 0.1728; //Changed 01/27/2022
        public const double RateOfReturn = 0.145152;
        public const int MaxStreetNumber = 55692;
        public const string HornbrodEOSAccount = "oqtr232h2c23";
    }
}
