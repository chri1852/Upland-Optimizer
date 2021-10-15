using System.Collections.Generic;

namespace Upland.Types.UplandApiTypes
{
    public class UplandForSalePropWrapper
    {
        public List<UplandForSaleProp> Properties { get; set; }
    }

    public class UplandForSaleProp
    {
        public long Prop_Id { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public double SortValue { get; set; }
        public string Owner { get; set; }

        public UplandForSaleProp Clone()
        {
            return new UplandForSaleProp
            {
                Prop_Id = this.Prop_Id,
                Price = this.Price,
                Currency = this.Currency,
                SortValue = this.SortValue,
                Owner = this.Owner
            };
        }
    }
}
