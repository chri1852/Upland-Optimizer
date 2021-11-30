using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Upland.Types.UplandApiTypes
{
    public class UplandForSalePropWrapper
    {
        public List<UplandForSaleProp> Properties { get; set; }
    }

    public class UplandForSaleProp : IEquatable<UplandForSaleProp>
    {
        public long Prop_Id { get; set; }
        public double Price { get; set; }
        public string Currency { get; set; }
        public string Owner { get; set; }

        public double SortValue 
        { 
            get
            {
                if (this.Currency == "USD")
                {
                    return this.Price * 1000;
                }
                else
                {
                    return this.Price;
                }    
            }
        }

        public UplandForSaleProp Clone()
        {
            return new UplandForSaleProp
            {
                Prop_Id = this.Prop_Id,
                Price = this.Price,
                Currency = this.Currency,
                Owner = this.Owner
            };
        }

        public bool Equals(UplandForSaleProp other)
        {
            return other.Prop_Id == this.Prop_Id;
        }
    }
}
