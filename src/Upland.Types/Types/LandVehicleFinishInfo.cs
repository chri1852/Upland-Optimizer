using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.Types
{
    public class LandVehicleFinishInfo
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Wheels { get; set; }
        public string DriveTrain { get; set; }
        public DateTime MintingEnd { get; set; }
        public int CarClassId { get; set; }
        public string CarClassName { get; set; }
        public int CarClassNumber { get; set; }
        public int Horsepower { get; set; }
        public int Weight { get; set; }
        public int Speed { get; set; }
        public int Acceleration { get; set; }
        public int Braking { get; set; }
        public int Handling { get; set; }
        public int EnergyEfficiency { get; set; }
        public int Reliability { get; set; }
        public int Durability { get; set; }
        public int Offroad { get; set; }
    }
}
