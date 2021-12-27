using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class OptimizerRunRequest
    {
        public string Username { get; set; }
        public int Level { get; set; }
        public int WhatIfCollectionId { get; set; }
        public int WhatIfNumProperties { get; set; }
        public double WhatIfAverageMonthlyUpx { get; set; }
        public List<int> ExcludeCollectionIds { get; set; }

        public bool StandardRun { get; set; }
        public bool WhatIfRun { get; set; }
        public bool ExcludeRun { get; set; }
        public bool DebugRun { get; set; }

        public OptimizerRunRequest(string username, int level = 7, bool debugRun = false) : this(username, level, -1, 0, 0, null, true, false, false, debugRun) { }
        public OptimizerRunRequest(string username, int whatIfCollectionId, int whatIfNumProperties, double whatIfAverageMonthylUpx, int level = 7, bool debugRun = false) : this(username, level, whatIfCollectionId, whatIfNumProperties, whatIfAverageMonthylUpx, null, false, true, false, debugRun) { }
        public OptimizerRunRequest(string username, List<int> excludeCollectionIds, int level = 7, bool debugRun = false) : this(username, level, -1, 0, 0, excludeCollectionIds, false, false, true, debugRun) { }

        public OptimizerRunRequest(
            string username,
            int level,
            int whatIfCollectionId,
            int whatIfNumProperties,
            double whatIfAverageMonthlyUpx,
            List<int> excludeCollectionIds,
            bool standardRun,
            bool whatIfRun,
            bool exclueRun,
            bool debugRun
        )
        {
            Username = username;
            Level = level;
            WhatIfCollectionId = whatIfCollectionId;
            WhatIfNumProperties = whatIfNumProperties;
            WhatIfAverageMonthlyUpx = whatIfAverageMonthlyUpx;
            ExcludeCollectionIds = excludeCollectionIds;
            StandardRun = standardRun;
            WhatIfRun = whatIfRun;
            ExcludeRun = exclueRun;
            DebugRun = debugRun;
        }
    }
}
