using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.InformationProcessor
{
    public static class HelperFunctions
    {
        public static string GetCollectionCategory(int category)
        {
            switch(category)
            {
                case 1:  { return "Standard";   }
                case 2:  { return "Limited";    }
                case 3:  { return "Exclusive";  }
                case 4:  { return "Rare";       }
                case 5:  { return "Ultra Rare"; }
                default: { return "Unknown";    }
            }
        }
    }
}
