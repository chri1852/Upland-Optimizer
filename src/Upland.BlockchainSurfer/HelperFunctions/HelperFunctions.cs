using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Upland.Types;

namespace Upland.BlockchainSurfer.HelperFunctions
{
    public static class HelperFunctions
    {
        public static string SusOutCityNameByMemoString(string memo)
        {
            string returnName = "";

            foreach (string name in Consts.Cities.Values)
            {
                if (memo.Contains(string.Format(" {0},", name)))
                {
                    returnName = name;
                    break;
                }

                if (memo.Contains(string.Format(" {0},", name.ToUpper())))
                {
                    returnName = name.ToUpper();
                    break;
                }
            }

            if (returnName.ToUpper() == "RUTHERFORD")
            {
                if (memo.ToUpper().Contains("EAST RUTHERFORD"))
                {
                    return "East Rutherford";
                }
                else
                {
                    return returnName;
                }
            }
            else if (returnName != "")
            {
                return returnName;
            }

            if (returnName == "" && Regex.Match(memo.ToUpper(), "KANSAS,").Success)
            {
                return "Kansas";
            }

            throw new Exception("Unknown City Detected");
        }

        public static int GetCityIdByName(string cityName)
        {
            if (Regex.Match(cityName.ToUpper(), "RUTHERFORD").Success)
            {
                return 13;
            }

            if (Regex.Match(cityName.ToUpper(), "INGLEWOOD").Success)
            {
                return 32;
            }

            if (Regex.Match(cityName.ToUpper(), "KANSAS").Success)
            {
                return 14;
            }

            // Since the sub cities get wrapped up to the main city we need to do some finagaling
            if (Consts.Cities.Where(c => c.Value.ToUpper() == cityName.ToUpper()).ToList().Count == 0)
            {
                throw new Exception("Unknown City Detected");
            }

            int cityId = Consts.Cities.Where(c => c.Value.ToUpper() == cityName.ToUpper()).First().Key;

            switch (cityId)
            {
                case 18:
                    return 5; // Fresno
                case 19:
                case 30:
                case 31:
                    return 7; // Oakland
                case 20:
                case 21:
                case 22:
                case 23:
                case 24:
                case 25:
                case 26:
                case 27:
                case 28:
                    return 16; // Nashville
                default:
                    return cityId;
            }
        }

        public static byte[] EncodeMetadata<T>(T metadata)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize<T>(metadata));
        }

        public static T DecodeMetadata<T>(byte[] metadata)
        {
            return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(metadata));
        }
    }
}
