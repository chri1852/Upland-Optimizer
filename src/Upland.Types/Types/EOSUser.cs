using System;

namespace Upland.Types.Types
{
    public class EOSUser
    {
        public int Id { get; set; }
        public string EOSAccount { get; set; }
        public string UplandUsername { get; set; }
        public DateTime Joined { get; set; }
        public decimal Spark { get; set; }
    }
}
