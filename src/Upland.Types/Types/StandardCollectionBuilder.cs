using System.Collections.Generic;
using System.Linq;

namespace Upland.CollectionOptimizer
{
    public class StandardCollectionBuilder
    {
        public int Id { get; set; }
        public List<long> PropIds { get; set; }
        public List<KeyValuePair<long, double>> Props { get; set; }
        public int NumberOfProps { get; set; }
        public double Boost { get; set; }

        public StandardCollectionBuilder Clone()
        {
            return new StandardCollectionBuilder
            {
                Id = this.Id,
                Props = new List<KeyValuePair<long, double>>(this.Props),
                NumberOfProps = this.NumberOfProps,
                Boost = this.Boost
            };
        }

        public double TotalMint(List<long> ignoreProps)
        {
            if (Props.Where(p => !ignoreProps.Contains(p.Key)).Count() < this.NumberOfProps)
            {
                return 0;
            }

            return Props.Where(p => !ignoreProps.Contains(p.Key)).Take(this.NumberOfProps).Sum(p => p.Value) * this.Boost;
        }
    }
}
