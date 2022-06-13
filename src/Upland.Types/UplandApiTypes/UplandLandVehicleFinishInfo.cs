using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types.UplandApiTypes
{
    public class UplandLandVehicleFinishInfo
    {
        public string ModelUrl { get; set; }
        public string TextureUrl { get; set; }
        public string TextureWheelsUrl { get; set; }
        public string Title { get; set; }
        public List<UplandLandVehicleStats> Stats { get; set; }
        public int NumWheels { get; set; }
        public string Floor { get; set; }
        public string Logo { get; set; }
        public string DriveTrain { get; set; }
        public string BgImageLandscapeSrc { get; set; }
        public string BgImagePortraitSrc { get; set; }
        public DateTime MintingEnd { get; set; }
        public UplandCarClass CarClass { get; set; }

        public int FinishId { get; set; }
    }

    public class UplandLandVehicleStats
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public string MeasurementUnits { get; set; }
        public int Diff { get; set; }
    }

    public class UplandCarClass
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
    }
}
