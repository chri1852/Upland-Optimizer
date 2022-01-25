using System;
using System.Collections.Generic;

namespace Upland.Types.Types
{
    public class OptimizerResults
    {
        public string Username { get; set; }
        public DateTime RunDateTime { get; set; }
        public long TimeToRunTicks { get; set; }
        public int QualityLevel { get; set; }
        public double BaseTotalIncome { get; set; }
        public double BoostedTotalIncome { get; set; }

        public List<OptimizerCollectionResult> OptimizedCollections { get; set; }
        public List<OptimizerCollectionResult> UnfilledCollections { get; set; }
        public List<OptimizerCollectionResult> UnoptimizedCollections { get; set; }
        public List<OptimizerCollectionResult> ExtraCollections { get; set; }
        public List<OptimizerCollectionResult> MissingCollections { get; set; }
    }

    public class OptimizerCollectionResult
    {
        public bool IsStandardCollection { get; set; }
        public string City { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public double Boost { get; set; }
        public int MissingProps { get; set; }

        public List<OptimizerCollectionProperty> Properties { get; set; }
    }

    public class OptimizerCollectionProperty
    {
        public string Address { get; set; }
        public double BaseIncome { get; set; }
    }
}
