using System.Collections.Generic;

namespace Upland.Types.Up2LandApiTypes
{
    public class Up2LandProperty
    {
        public long Prop_Id { get; set; }
        public string Owner { get; set; }
        public bool FSA { get; set; }
        public int Street_Id { get; set; }
        public int State_Id { get; set; }
        public string Full_Address { get; set; }
        public int City_Id { get; set; }
        public int Neighborhood_Id { get; set; }
        public double Mint_Price { get; set; }
        public List<Up2LandCollection> collections { get; set; }
    }
}
