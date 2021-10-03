using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Types
{
    public static class Consts
    {
        public static readonly string DataFolder = @"C:\Users\chri1\Desktop\Upland\Scripts\Data";
        public static readonly string OuputFolder = @"C:\Users\chri1\Desktop\Upland\Optimizations";
        public static readonly string LocalDBConnectionString = @"Data Source=DESKTOP-BK6KAFH\SQLEXPRESS;Initial Catalog=UplandLocalData;Integrated Security=True;";

        public static readonly string AuthToken = @"eyJhbGciOiJIUzI1NiIsInR5cCI6ImFjY2VzcyJ9.eyJ1c2VySWQiOiIzZDk1ZTk0MC0xMDhkLTExZWItYmQ3OS1iZmE2NzhmODI5YzciLCJ2YWxpZGF0aW9uVG9rZW4iOiJVeklnZWh1TUxSYWwwV05TbGY0Y1lITU9aVUNSclZwOFhWVEVGZklkS29HIiwiaWF0IjoxNjMxNjIxMzE1LCJleHAiOjE2NjMxNzg5MTUsImlzcyI6ImZlYXRoZXJzIiwic3ViIjoiM2Q5NWU5NDAtMTA4ZC0xMWViLWJkNzktYmZhNjc4ZjgyOWM3IiwianRpIjoiZGE2MTg5OTItODViNi00ZjNhLTlhMzUtOTY2MjBlYjQyYWZkIn0.INOS3cNg5PXn1gLLEZsPmpjdkbbvuefY2wi7b1tpzQg";

        public static readonly double ReturnRate = 0.1728;

        public static readonly string CityPro = "City Pro";
        public static readonly string KingOfTheStreet = "King of the Street";
        public static readonly string Newbie = "Newbie";

        public static readonly double CityProBoost = 1.4;
        public static readonly double KingOfTheStreetBoost = 1.3;
        public static readonly double NewbieBoost = 1.1;

        public static readonly int CityProId = 21;
        public static readonly int KingOfTheStreetId = 1;
        public static readonly int NewbieId = 7;

        public static readonly List<int> StandardAndCityCollectionIds = new List<int>
        {
            1,   // King of the Street
            7,   // Newbie
            21,  // City Pro
            11,  // San Franciscan
            22,  // New Yorker
            57,  // The Fresno
            72,  // Brooklyner
            74,  // The East Bay
            89,  // Staten Islander
            102, // Bakersfielder
            135, // Chicagoan
            136, // Clevelander
            152, // Santa Claran
            183  // Kansas City
        };
}
}
