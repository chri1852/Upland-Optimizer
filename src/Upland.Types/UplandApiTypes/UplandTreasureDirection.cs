using System;
using System.Collections.Generic;
using System.Text;
using Upland.Types.Enums;

namespace Upland.Types.UplandApiTypes
{
    public class UplandTreasureDirection
    {
        public int hidden { get; set; }

        public List<UplandTreasureArrow> arrows { get; set; }
    }

    public class UplandTreasureArrow
    {
        private static readonly double QUARTER_PI = Math.PI / 4.0;

        public UplandTreasureSubArrow arrow { get; set; }
        public decimal direction { get; set; }
        public int id { get; set; }
        public bool is_locked { get; set; }
        public List<UplandTreasureRing> rings { get; set; }
        
        public int minimumDistance
        {
            get
            {
                if (this.rings.Count == 1 && this.arrow.speed == 2)
                {
                    return 10000;
                }
                else if (this.rings.Count == 2 && this.arrow.speed == 2)
                {
                    return 3000;
                }
                else if (this.rings.Count == 3 && this.arrow.speed == 1)
                {
                    return 1000;
                }
                else if (this.rings.Count == 4 && this.arrow.speed == 0.83)
                {
                    return 500;
                }
                else if (this.rings.Count == 4 && this.arrow.speed == 0.67)
                {
                    return 200;
                }
                else if (this.rings.Count == 6 && this.arrow.speed == 0.33)
                {
                    return 50;
                }
                else if (this.rings.Count == 7 && this.arrow.speed == 0.17)
                {
                    return 0;
                }
                else if (this.rings.Count == 8)
                {
                    return -1;
                }
                else
                {
                    throw new Exception("Unexpection Arrow and Ring Configuration");
                }
            }
        }

        public int maximumDistance
        {
            get
            {
                if (this.rings.Count == 1 && this.arrow.speed == 2)
                {
                    return int.MaxValue;
                }
                else if (this.rings.Count == 2 && this.arrow.speed == 2)
                {
                    return 10000;
                }
                else if (this.rings.Count == 3 && this.arrow.speed == 1)
                {
                    return 3000;
                }
                else if (this.rings.Count == 4 && this.arrow.speed == 0.83)
                {
                    return 1000;
                }
                else if (this.rings.Count == 4 && this.arrow.speed == 0.67)
                {
                    return 500;
                }
                else if (this.rings.Count == 6 && this.arrow.speed == 0.33)
                {
                    return 200;
                }
                else if (this.rings.Count == 7 && this.arrow.speed == 0.17)
                {
                    return 50;
                }
                else if (this.rings.Count == 8)
                {
                    return -1;
                }
                else
                {
                    throw new Exception("Unexpection Arrow and Ring Configuration");
                }
            }
        }

        public bool propHasTreasure
        {
            get
            {
                return this.rings.Count == 8;
            }
        }

        public long propertyId { get; set; }

        public TreasureTypeEnum treasureType
        {
            get
            {
                switch(this.arrow.color)
                {
                    case "#005AB3":
                        return TreasureTypeEnum.Standard;
                    case "#FFA800":
                        return TreasureTypeEnum.Exclusive;
                    case "#76379D":
                        return TreasureTypeEnum.Limited;
                    case "#5F65FE":
                        return TreasureTypeEnum.Spark;
                    default:
                        throw new Exception("Unknown Treasure Type");
                }
            }
        }

        public bool IsAngleValid(double angle)
        {
            double roundedDirection = Math.Round((double)this.direction, 2, MidpointRounding.AwayFromZero);

            switch (roundedDirection)
            {
                case 0:
                    return angle >= -QUARTER_PI && angle <= QUARTER_PI;
                case 0.79:
                    return angle >= 0 && angle <= 2 * QUARTER_PI;
                case 1.57:
                    return angle >= QUARTER_PI && angle <= 3 * QUARTER_PI;
                case 2.36:
                    return angle >= 2 * QUARTER_PI && angle <= Math.PI;
                case 3.14:
                    return (angle >= 3 * QUARTER_PI && angle <= Math.PI) || (angle <= -3 * QUARTER_PI && angle >= -Math.PI);
                case -2.36:
                    return angle <= -2 * QUARTER_PI && angle >= -Math.PI;
                case -1.57:
                    return angle <= -QUARTER_PI && angle >= -3 * QUARTER_PI;
                case -0.79:
                    return angle <= 0 && angle >= -2 * QUARTER_PI;
                case -3.14:
                    return (angle >= 3 * QUARTER_PI && angle <= Math.PI) || (angle <= -3 * QUARTER_PI && angle >= -Math.PI);
                default:
                    throw new Exception("Invalid Angle");
            }
        }

        public string TextDirection
        {
            get
            {
                double roundedDirection = Math.Round((double)this.direction, 2, MidpointRounding.AwayFromZero);

                switch (roundedDirection)
                {
                    case 0:
                        return "N";
                    case 0.79:
                        return "NE";
                    case 1.57:
                        return "E";
                    case 2.36:
                        return "SE";
                    case 3.14:
                        return "S";
                    case -2.36:
                        return "SW";
                    case -1.57:
                        return "W";
                    case -0.79:
                        return "NW";
                    case -3.14:
                        return "S";
                    default:
                        throw new Exception("Invalid Angle");
                }
            }
        }
    }

    public class UplandTreasureSubArrow
    {
        public string color { get; set; }
        public double speed { get; set; }
    }

    public class UplandTreasureRing
    {

    }
}
