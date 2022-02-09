using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Upland.Infrastructure.LocalData;
using Upland.Interfaces.Managers;
using Upland.Interfaces.Processors;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public class MappingProcessor : IMappingProcessor
    {
        private ILocalDataManager _localDataManager;
        private IProfileAppraiser _profileAppraiser;

        private List<Color> _standardKey;
        private List<Color> _colorBlindKey;

        private readonly List<string> _validTypes;

        private readonly Font _smallFont;
        private readonly Font _largeFont;

        public MappingProcessor(ILocalDataManager localDataManager, IProfileAppraiser profileAppraiser)
        {
            _localDataManager = localDataManager;
            _profileAppraiser = profileAppraiser;

            BuildStandardKey();
            BuildColorBlindKey();

            _validTypes = new List<string>();
            _validTypes.Add("SOLD");
            _validTypes.Add("SOLDNONFSA");
            _validTypes.Add("FLOOR");
            _validTypes.Add("FLOORUSD");
            _validTypes.Add("PERUP2");
            _validTypes.Add("BUILDINGS");
            _validTypes.Add("PERCENTBUILT");

            _smallFont = new Font(SystemFonts.Find("DejaVu Sans Mono"), 20);
            _largeFont = new Font(SystemFonts.Find("DejaVu Sans Mono"), 30);
        }

        public bool IsValidType(string type)
        {
            if(_validTypes.Contains(type.ToUpper()))
            {
                return true;
            }
            return false;
        }

        public List<string> GetValidTypes()
        {
            return _validTypes;
        }

        public void SaveMap(Image<Rgba32> map, string fileName)
        {
            if (!Directory.Exists(Path.Combine("GeneratedMaps")))
            {
                Directory.CreateDirectory(Path.Combine("GeneratedMaps"));
            }
            map.SaveAsPng(Path.Combine("GeneratedMaps", string.Format("{0}.png", fileName)));
        }

        public void DeleteSavedMap(string fileName)
        {
            File.Delete(Path.Combine("GeneratedMaps", string.Format("{0}.png", fileName)));
        }

        public string GetMapLocaiton(string fileName)
        {
            return Path.Combine("GeneratedMaps", string.Format("{0}.png", fileName));
        }

        public string CreateMap(int cityId, string type, int registeredUserId, bool colorBlind, List<string> customColors)
        {
            string typeString;
            Image<Rgba32> map;
            Image<Rgba32> key;

            List<Color> customKey = null;

            if (customKey != null && customKey.Count == 11)
            {
                customKey = BuildCustomKey(customColors);
            }

            if (type.ToUpper() == "SOLD")
            {
                typeString = "Sold Out";
                map = CreateSoldOutMap(cityId, colorBlind, false, customKey);
                key = BuildStandardKey(colorBlind, customKey);
            }
            else if (type.ToUpper() == "SOLDNONFSA")
            {
                typeString = "Sold Out Non FSA";
                map = CreateSoldOutMap(cityId, colorBlind, true, customKey);
                key = BuildStandardKey(colorBlind, customKey);
            }
            else if (type.ToUpper() == "FLOOR" || type.ToUpper() == "FLOORUSD")
            {
                string currency = type.ToUpper() == "FLOOR" ? "UPX" : "USD";

                typeString = type.ToUpper() == "FLOOR" ? "UPX Floor" : "USD Floor";

                List<UplandForSaleProp> forSaleProps = _localDataManager
                    .GetPropertiesForSale_City(cityId, false)
                    .Where(p => p.Currency == currency).ToList();

                Dictionary<long, Property> propertyDictionary = _localDataManager
                    .GetProperties(forSaleProps.GroupBy(p => p.Prop_Id).Select(g => g.First().Prop_Id).ToList())
                    .ToDictionary(p => p.Id, p => p);

                Dictionary<int, double> lowestHoodPrice = _localDataManager.GetNeighborhoods().Where(n => n.CityId == cityId).ToDictionary(n => n.Id, n => double.MaxValue);

                foreach (UplandForSaleProp prop in forSaleProps)
                {
                    if (propertyDictionary[prop.Prop_Id].NeighborhoodId == null)
                    {
                        continue;
                    }

                    int neighborhoodId = propertyDictionary[prop.Prop_Id].NeighborhoodId.Value;

                    if (lowestHoodPrice.ContainsKey(neighborhoodId))
                    {
                        if (lowestHoodPrice[neighborhoodId] > prop.Price)
                        {
                            lowestHoodPrice[neighborhoodId] = prop.Price;
                        }
                    }
                }

                key = BuildFloorKey(lowestHoodPrice, colorBlind, customKey);
                map = CreateFloorMap(cityId, lowestHoodPrice, colorBlind, customKey);
            }
            else if (type.ToUpper() == "PERUP2")
            {
                typeString = "Average Price Per UP2 From Past 4 Weeks";

                List<Neighborhood> neighborhoods = _localDataManager.GetNeighborhoods().Where(n => n.CityId == cityId).ToList();
                Dictionary<int, double> marketData = _profileAppraiser.GetNeighborhoodPricePerUP2();

                Dictionary<int, double> neighborhoodPerUp2 = new Dictionary<int, double>();

                foreach (Neighborhood neighborhood in neighborhoods)
                {
                    if (!marketData.ContainsKey(neighborhood.Id))
                    {
                        neighborhoodPerUp2.Add(neighborhood.Id, double.MaxValue);
                    }
                    else
                    {
                        neighborhoodPerUp2.Add(neighborhood.Id, marketData[neighborhood.Id]);
                    }
                }

                key = BuildPerUP2Key(neighborhoodPerUp2, colorBlind, customKey);
                map = CreatePerUP2Map(cityId, neighborhoodPerUp2, colorBlind, customKey);
            }
            else if (type.ToUpper() == "BUILDINGS")
            {
                Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);
                List<Neighborhood> neighborhoods = _localDataManager.GetNeighborhoods().Where(n => n.CityId == cityId).ToList();
                Dictionary<int, int> neighborhoodStructureCount = _localDataManager
                    .GetPropertiesByCityId(cityId)
                    .Where(p => propertyStructures.ContainsKey(p.Id))
                    .GroupBy(p => p.NeighborhoodId)
                    .ToDictionary(g => g.First().NeighborhoodId.Value, g => g.Count());

                foreach (Neighborhood neighborhood in neighborhoods)
                {
                    // Add a zero entry if no structures
                    if (!neighborhoodStructureCount.ContainsKey(neighborhood.Id))
                    {
                        neighborhoodStructureCount.Add(neighborhood.Id, 0);
                    }
                }

                typeString = "Completed Buildings";
                map = CreateBuildingCountMap(cityId, colorBlind, neighborhoodStructureCount, customKey);
                key = BuildBuildingCountKey(neighborhoodStructureCount, colorBlind, customKey);
            }
            else if (type.ToUpper() == "PERCENTBUILT")
            {
                Dictionary<long, string> propertyStructures = _localDataManager.GetPropertyStructures().ToDictionary(p => p.PropertyId, p => p.StructureType);
                List<Property> cityProperties = _localDataManager.GetPropertiesByCityId(cityId);
                Dictionary<int, int> neighborhoodPropertyCounts = cityProperties
                    .Where(p => p.NeighborhoodId.HasValue)
                    .GroupBy(p => p.NeighborhoodId)
                    .ToDictionary(g => g.First().NeighborhoodId.Value, g => g.Count());

                List<Neighborhood> neighborhoods = _localDataManager.GetNeighborhoods().Where(n => n.CityId == cityId).ToList();
                Dictionary<int, double> neighborhoodStructurePercent = cityProperties
                    .Where(p => propertyStructures.ContainsKey(p.Id))
                    .GroupBy(p => p.NeighborhoodId)
                    .ToDictionary(g => g.First().NeighborhoodId.Value, g => g.Count()*100.0/neighborhoodPropertyCounts[g.First().NeighborhoodId.Value]);

                foreach (Neighborhood neighborhood in neighborhoods)
                {
                    // Add a zero entry if no structures
                    if (!neighborhoodStructurePercent.ContainsKey(neighborhood.Id))
                    {
                        neighborhoodStructurePercent.Add(neighborhood.Id, 0);
                    }
                }

                typeString = "Percent Built";
                map = CreateBuildingPercentMap(cityId, colorBlind, neighborhoodStructurePercent, customKey);
                key = BuildBuildingPercentKey(neighborhoodStructurePercent, colorBlind, customKey);
            }
            else
            {
                throw new Exception("Invalid Map Type Selected");
            }

            Image<Rgba32> header = CreateHeader(cityId, typeString);
            Image<Rgba32> footer = CreateFooter();

            Image<Rgba32> combinedMap = new Image<Rgba32>(Math.Max(Math.Max(map.Width + key.Width + 4, header.Width), footer.Width), map.Height + 110);

            combinedMap.Mutate(x => x.BackgroundColor(Color.White));
            combinedMap.Mutate(x => x.DrawImage(map, new Point(0, header.Height + 1), 1));
            combinedMap.Mutate(x => x.DrawImage(key, new Point(map.Width + 1, header.Height + 1), 1));

            combinedMap.Mutate(x => x.DrawImage(header, new Point((combinedMap.Width - header.Width) / 2, 0), 1));
            combinedMap.Mutate(x => x.DrawImage(footer, new Point((combinedMap.Width - footer.Width) / 2, combinedMap.Height + -50), 1));


            string filename = string.Format("{0}_{1}_{2}_{3}", Consts.Cities[cityId].Replace(" ", ""), type.ToUpper(), registeredUserId, DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            SaveMap(combinedMap, filename);

            return filename;
        }
        
        private Image<Rgba32> CreateFloorMap(int cityId, Dictionary<int, double> lowestHoodPrice, bool colorBlind, List<Color> customKey)
        {
            Image<Rgba32> cityMap = LoadBlankMapByCityId(cityId);

            List<Color> colorKeys = new List<Color>();

            if (customKey != null)
            {
                colorKeys = customKey;
            }
            else
            {
                colorKeys = colorBlind ? _colorBlindKey : _standardKey;
            }

            Dictionary<Color, Neighborhood> colorDictionary = _localDataManager.GetNeighborhoods()
                .Where(n => n.CityId == cityId)
                .ToDictionary(n => Color.FromRgb((byte)n.RGB[0], (byte)n.RGB[1], (byte)n.RGB[2]), n => n);

            Image<Rgba32> newBitmap = new Image<Rgba32>(cityMap.Width, cityMap.Height);

            List<double> formattedOrderedPrices = lowestHoodPrice.GroupBy(l => l.Value).Select(d => d.First().Value).OrderBy(p => p).ToList();
            double numberInbetween = (formattedOrderedPrices.Where(m => m != double.MaxValue).ToList().Count-1) / 9.0;
            double max = formattedOrderedPrices.Where(m => m != double.MaxValue).Max();

            Color actualColor;
            int neigborhoodId;
            double price;

            for (int i = 0; i < cityMap.Width; i++)
            {
                for (int j = 0; j < cityMap.Height; j++)
                {
                    actualColor = cityMap[i, j];

                    if (colorDictionary.ContainsKey(actualColor))
                    {
                        neigborhoodId = colorDictionary[actualColor].Id;
                        price = lowestHoodPrice[neigborhoodId];

                        if (price == double.MaxValue)
                        {
                            newBitmap[i, j] = colorKeys[10];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 9)])
                        {
                            newBitmap[i, j] = colorKeys[9];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 8)])
                        {
                            newBitmap[i, j] = colorKeys[8];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 7)])
                        {
                            newBitmap[i, j] = colorKeys[7];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 6)])
                        {
                            newBitmap[i, j] = colorKeys[6];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 5)])
                        {
                            newBitmap[i, j] = colorKeys[5];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 4)])
                        {
                            newBitmap[i, j] = colorKeys[4];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 3)])
                        {
                            newBitmap[i, j] = colorKeys[3];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 2)])
                        {
                            newBitmap[i, j] = colorKeys[2];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 1)])
                        {
                            newBitmap[i, j] = colorKeys[1];
                        }
                        else
                        {
                            newBitmap[i, j] = colorKeys[0];
                        }
                    }
                    else
                    {
                        newBitmap[i, j] = actualColor;
                    }
                }
            }

            return newBitmap;
        }
        
        private Image<Rgba32> CreatePerUP2Map(int cityId, Dictionary<int, double> neighborhoodPerUp2, bool colorBlind, List<Color> customKey)
        {
            Image<Rgba32> cityMap = LoadBlankMapByCityId(cityId);

            List<Color> colorKeys = new List<Color>();

            if (customKey != null)
            {
                colorKeys = customKey;
            }
            else
            {
                colorKeys = colorBlind ? _colorBlindKey : _standardKey;
            }

            Dictionary<Color, Neighborhood> colorDictionary = _localDataManager.GetNeighborhoods()
                .Where(n => n.CityId == cityId)
                .ToDictionary(n => Color.FromRgb((byte)n.RGB[0], (byte)n.RGB[1], (byte)n.RGB[2]), n => n);

            Image<Rgba32> newBitmap = new Image<Rgba32>(cityMap.Width, cityMap.Height);

            List<double> formattedOrderedPrices = neighborhoodPerUp2.GroupBy(l => l.Value).Select(d => d.First().Value).OrderBy(p => p).ToList();
            double numberInbetween = formattedOrderedPrices.Where(m => m != double.MaxValue).ToList().Count / 10.0;
            double max = formattedOrderedPrices.Where(m => m != double.MaxValue).Max();

            Color actualColor;
            int neigborhoodId;
            double price;

            for (int i = 0; i < cityMap.Width; i++)
            {
                for (int j = 0; j < cityMap.Height; j++)
                {
                    actualColor = cityMap[i, j];

                    if (colorDictionary.ContainsKey(actualColor))
                    {
                        neigborhoodId = colorDictionary[actualColor].Id;
                        price = neighborhoodPerUp2[neigborhoodId];

                        if (price == double.MaxValue)
                        {
                            newBitmap[i, j] = colorKeys[10];
                        }
                        else if (price >= max)
                        {
                            newBitmap[i, j] = colorKeys[9];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 9)])
                        {
                            newBitmap[i, j] = colorKeys[8];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 8)])
                        {
                            newBitmap[i, j] = colorKeys[7];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 7)])
                        {
                            newBitmap[i, j] = colorKeys[6];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 6)])
                        {
                            newBitmap[i, j] = colorKeys[5];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 5)])
                        {
                            newBitmap[i, j] = colorKeys[4];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 4)])
                        {
                            newBitmap[i, j] = colorKeys[3];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 3)])
                        {
                            newBitmap[i, j] = colorKeys[2];
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 2)])
                        {
                            newBitmap[i, j] = colorKeys[1];
                        }
                        else
                        {
                            newBitmap[i, j] = colorKeys[0];
                        }
                    }
                    else
                    {
                        newBitmap[i, j] = actualColor;
                    }
                }
            }

            return newBitmap;
        }
        
        private Image<Rgba32> CreateSoldOutMap(int cityId, bool colorBlind, bool nonFSAOnly, List<Color> customKey)
        {
            Image<Rgba32> cityMap = LoadBlankMapByCityId(cityId);

            List<Color> colorKeys = new List<Color>();

            if (customKey != null)
            {
                colorKeys = customKey;
            }
            else
            {
                colorKeys = colorBlind ? _colorBlindKey : _standardKey;
            };

            Dictionary<int, CollatedStatsObject> neighborhoodStats = _localDataManager.GetNeighborhoodStats()
                .ToDictionary(n => n.Id, n => n);

            Dictionary<Color, Neighborhood> colorDictionary = _localDataManager.GetNeighborhoods()
                .Where(n => n.CityId == cityId)
                .ToDictionary(n => Color.FromRgb((byte)n.RGB[0], (byte)n.RGB[1], (byte)n.RGB[2]), n => n);

            Image<Rgba32> newBitmap = new Image<Rgba32>(cityMap.Width, cityMap.Height);

            Color actualColor;
            double percentMinted;

            for (int i = 0; i < cityMap.Width; i++)
            {
                for (int j = 0; j < cityMap.Height; j++)
                {
                    actualColor = cityMap[i, j];

                    if (colorDictionary.ContainsKey(actualColor))
                    {
                        percentMinted = nonFSAOnly 
                            ? neighborhoodStats[colorDictionary[actualColor].Id].PercentNonFSAMinted
                            : neighborhoodStats[colorDictionary[actualColor].Id].PercentMinted;

                        if (percentMinted == 100)
                        {
                            newBitmap[i, j] = colorKeys[10];
                        }
                        else if (percentMinted >= 90)
                        {
                            newBitmap[i, j] = colorKeys[9];
                        }
                        else if (percentMinted >= 80)
                        {
                            newBitmap[i, j] = colorKeys[8];
                        }
                        else if (percentMinted >= 70)
                        {
                            newBitmap[i, j] = colorKeys[7];
                        }
                        else if (percentMinted >= 60)
                        {
                            newBitmap[i, j] = colorKeys[6];
                        }
                        else if (percentMinted >= 50)
                        {
                            newBitmap[i, j] = colorKeys[5];
                        }
                        else if (percentMinted >= 40)
                        {
                            newBitmap[i, j] = colorKeys[4];
                        }
                        else if (percentMinted >= 30)
                        {
                            newBitmap[i, j] = colorKeys[3];
                        }
                        else if (percentMinted >= 20)
                        {
                            newBitmap[i, j] = colorKeys[2];
                        }
                        else if (percentMinted >= 10)
                        {
                            newBitmap[i, j] = colorKeys[1];
                        }
                        else
                        {
                            newBitmap[i, j] = colorKeys[0];
                        }
                    }
                    else
                    {
                        newBitmap[i, j] = actualColor;
                    }
                }
            }

            return newBitmap;
        }

        private Image<Rgba32> CreateBuildingCountMap(int cityId, bool colorBlind, Dictionary<int, int> neighborhoodStructureCount, List<Color> customKey)
        {
            Image<Rgba32> cityMap = LoadBlankMapByCityId(cityId);

            List<Color> colorKeys = new List<Color>();

            if (customKey != null)
            {
                colorKeys = customKey;
            }
            else
            {
                colorKeys = colorBlind ? _colorBlindKey : _standardKey;
            }

            Dictionary<Color, Neighborhood> colorDictionary = _localDataManager.GetNeighborhoods()
                .Where(n => n.CityId == cityId)
                .ToDictionary(n => Color.FromRgb((byte)n.RGB[0], (byte)n.RGB[1], (byte)n.RGB[2]), n => n);

            Image<Rgba32> newBitmap = new Image<Rgba32>(cityMap.Width, cityMap.Height);

            List<int> formattedOrderedList = neighborhoodStructureCount.GroupBy(l => l.Value).Select(d => d.First().Value).OrderBy(p => p).ToList();
            double numberInbetween = (formattedOrderedList.Count - 1) / 10.0;
            double max = formattedOrderedList.Max();

            Color actualColor;
            int neigborhoodId;
            int buildingCount;

            for (int i = 0; i < cityMap.Width; i++)
            {
                for (int j = 0; j < cityMap.Height; j++)
                {
                    actualColor = cityMap[i, j];

                    if (colorDictionary.ContainsKey(actualColor))
                    {
                        neigborhoodId = colorDictionary[actualColor].Id;
                        buildingCount = neighborhoodStructureCount[neigborhoodId];

                        if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 10)])
                        {
                            newBitmap[i, j] = colorKeys[10];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 9)])
                        {
                            newBitmap[i, j] = colorKeys[9];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 8)])
                        {
                            newBitmap[i, j] = colorKeys[8];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 7)])
                        {
                            newBitmap[i, j] = colorKeys[7];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 6)])
                        {
                            newBitmap[i, j] = colorKeys[6];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 5)])
                        {
                            newBitmap[i, j] = colorKeys[5];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 4)])
                        {
                            newBitmap[i, j] = colorKeys[4];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 3)])
                        {
                            newBitmap[i, j] = colorKeys[3];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 2)])
                        {
                            newBitmap[i, j] = colorKeys[2];
                        }
                        else if (buildingCount >= formattedOrderedList[(int)Math.Floor(numberInbetween * 1)])
                        {
                            newBitmap[i, j] = colorKeys[1];
                        }
                        else
                        {
                            newBitmap[i, j] = colorKeys[0];
                        }
                    }
                    else
                    {
                        newBitmap[i, j] = actualColor;
                    }
                }
            }

            return newBitmap;
        }

        private Image<Rgba32> CreateBuildingPercentMap(int cityId, bool colorBlind, Dictionary<int, double> neighborhoodStructurePercent, List<Color> customKey)
        {
            Image<Rgba32> cityMap = LoadBlankMapByCityId(cityId);

            List<Color> colorKeys = new List<Color>();

            if (customKey != null)
            {
                colorKeys = customKey;
            }
            else
            {
                colorKeys = colorBlind ? _colorBlindKey : _standardKey;
            }

            Dictionary<Color, Neighborhood> colorDictionary = _localDataManager.GetNeighborhoods()
                .Where(n => n.CityId == cityId)
                .ToDictionary(n => Color.FromRgb((byte)n.RGB[0], (byte)n.RGB[1], (byte)n.RGB[2]), n => n);

            Image<Rgba32> newBitmap = new Image<Rgba32>(cityMap.Width, cityMap.Height);

            List<double> formattedOrderedList = neighborhoodStructurePercent.GroupBy(l => l.Value).Select(d => d.First().Value).OrderBy(p => p).ToList();
            double numberInbetween = (formattedOrderedList.Count - 1) / 10.0;
            double max = formattedOrderedList.Max();

            Color actualColor;
            int neigborhoodId;
            double buildingPercent;

            for (int i = 0; i < cityMap.Width; i++)
            {
                for (int j = 0; j < cityMap.Height; j++)
                {
                    actualColor = cityMap[i, j];

                    if (colorDictionary.ContainsKey(actualColor))
                    {
                        neigborhoodId = colorDictionary[actualColor].Id;
                        buildingPercent = neighborhoodStructurePercent[neigborhoodId];

                        if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 10)])
                        {
                            newBitmap[i, j] = colorKeys[10];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 9)])
                        {
                            newBitmap[i, j] = colorKeys[9];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 8)])
                        {
                            newBitmap[i, j] = colorKeys[8];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 7)])
                        {
                            newBitmap[i, j] = colorKeys[7];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 6)])
                        {
                            newBitmap[i, j] = colorKeys[6];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 5)])
                        {
                            newBitmap[i, j] = colorKeys[5];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 4)])
                        {
                            newBitmap[i, j] = colorKeys[4];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 3)])
                        {
                            newBitmap[i, j] = colorKeys[3];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 2)])
                        {
                            newBitmap[i, j] = colorKeys[2];
                        }
                        else if (buildingPercent >= formattedOrderedList[(int)Math.Floor(numberInbetween * 1)])
                        {
                            newBitmap[i, j] = colorKeys[1];
                        }
                        else
                        {
                            newBitmap[i, j] = colorKeys[0];
                        }
                    }
                    else
                    {
                        newBitmap[i, j] = actualColor;
                    }
                }
            }

            return newBitmap;
        }

        private Image<Rgba32> BuildFloorKey(Dictionary<int, double> lowestHoodPrice, bool colorBlind, List<Color> customKey)
        {
            List<string> formattedOrderedPrices = lowestHoodPrice
                .Where(l => l.Value != double.MaxValue)
                .GroupBy(l => l.Value)
                .Select(d => d.First().Value)
                .OrderBy(p => p)
                .ToList().Select(p => string.Format("{0:N2}", p)).ToList();
            int maxPriceLength = formattedOrderedPrices.Select(p => string.Format("{0:N2}", p)).Max(v => v.Length);
            double numberInbetween = (formattedOrderedPrices.Count-1) / 9.0;

            List<string> keyTextStrings = new List<string>();
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 0)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 1)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 2)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 3)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 4)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 5)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 6)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 7)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 8)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 9)].PadLeft(maxPriceLength)));
            keyTextStrings.Add("No Floor");

            return BuildKey(colorBlind, keyTextStrings, customKey);
        }

        private Image<Rgba32> BuildPerUP2Key(Dictionary<int, double> neighborhoodPerUp2, bool colorBlind, List<Color> customKey)
        {
            List<string> formattedOrderedPrices = neighborhoodPerUp2
                .Where(l => l.Value != double.MaxValue)
                .GroupBy(l => l.Value)
                .Select(d => d.First().Value)
                .OrderBy(p => p)
                .ToList().Select(p => string.Format("{0:N2}", p)).ToList();
            int maxPriceLength = formattedOrderedPrices.Select(p => string.Format("{0:N2}", p)).Max(v => v.Length);
            double numberInbetween = (formattedOrderedPrices.Count - 1) / 9.0;

            List<string> keyTextStrings = new List<string>();
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 0)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 1)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 2)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 3)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 4)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 5)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 6)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 7)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 8)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" >= {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 9)].PadLeft(maxPriceLength)));
            keyTextStrings.Add("Low Market Data");

            return BuildKey(colorBlind, keyTextStrings, customKey);
        }

        private Image<Rgba32> BuildBuildingCountKey(Dictionary<int, int> neighborhoodStructureCount, bool colorBlind, List<Color> customKey)
        {
            List<string> formattedOrderedList= neighborhoodStructureCount
                .GroupBy(l => l.Value)
                .Select(d => d.First().Value)
                .OrderBy(p => p)
                .ToList().Select(p => string.Format("{0:N0}", p)).ToList();
            int maxLength = formattedOrderedList.Select(p => string.Format("{0:N0}", p)).Max(v => v.Length);
            double numberInbetween = (formattedOrderedList.Count - 1) / 10.0;

            List<string> keyTextStrings = new List<string>();
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 0)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 1)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 2)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 3)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 4)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 5)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 6)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 7)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 8)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 9)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0} Buildings", formattedOrderedList[(int)Math.Floor(numberInbetween * 10)].PadLeft(maxLength)));

            return BuildKey(colorBlind, keyTextStrings, customKey);
        }

        private Image<Rgba32> BuildBuildingPercentKey(Dictionary<int, double> neighborhoodStructurePercent, bool colorBlind, List<Color> customKey)
        {
            List<string> formattedOrderedList = neighborhoodStructurePercent
                .GroupBy(l => l.Value)
                .Select(d => d.First().Value)
                .OrderBy(p => p)
                .ToList().Select(p => string.Format("{0:N2}", p)).ToList();
            int maxLength = formattedOrderedList.Select(p => string.Format("{0:N2}", p)).Max(v => v.Length);
            double numberInbetween = (formattedOrderedList.Count - 1) / 10.0;

            List<string> keyTextStrings = new List<string>();
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 0)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 1)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 2)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 3)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 4)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 5)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 6)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 7)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 8)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 9)].PadLeft(maxLength)));
            keyTextStrings.Add(string.Format(" >= {0}%", formattedOrderedList[(int)Math.Floor(numberInbetween * 10)].PadLeft(maxLength)));

            return BuildKey(colorBlind, keyTextStrings, customKey);
        }

        private Image<Rgba32> BuildStandardKey(bool colorBlind, List<Color> customKey)
        {
            List<string> keyTextStrings = new List<string>();
            keyTextStrings.Add("     < 10%");
            keyTextStrings.Add("10% -- 20%");
            keyTextStrings.Add("20% -- 30%");
            keyTextStrings.Add("30% -- 40%");
            keyTextStrings.Add("40% -- 50%");
            keyTextStrings.Add("50% -- 60%");
            keyTextStrings.Add("60% -- 70%");
            keyTextStrings.Add("70% -- 80%");
            keyTextStrings.Add("80% -- 90%");
            keyTextStrings.Add("90% -- 99%");
            keyTextStrings.Add("Sold Out");

            return BuildKey(colorBlind, keyTextStrings, customKey);
        }

        private Image<Rgba32> BuildKey(bool colorBlind, List<string> keyTextStrings, List<Color> customKey)
        {
            List<Color> colorKeys = new List<Color>();

            if (customKey != null)
            {
                colorKeys = customKey;
            }
            else
            {
                colorKeys = colorBlind ? _colorBlindKey : _standardKey;
            }

            Image<Rgba32> bmp = new Image<Rgba32>((keyTextStrings.Max(t => t.Length) * 14) + 4, 279);

            string lastKeyString = keyTextStrings[0];
            int lastHeightchange_i = 0;
            int numOfRows = 0;
            RectangleF rectf;

            for (int i = 0; i < colorKeys.Count; i++)
            {
                if (lastKeyString == keyTextStrings[i])
                {
                    numOfRows++;
                    continue;
                }
                else
                {
                    bmp.Mutate(x => x.FillPolygon(colorKeys[i - 1],  new PointF[] {
                        new PointF(2, (lastHeightchange_i * 25) + 2), new PointF(bmp.Width-2, (lastHeightchange_i * 25) + 2), 
                        new PointF(bmp.Width - 2, (lastHeightchange_i * 25) + 2 + (25 * numOfRows)), new PointF(2, (lastHeightchange_i * 25) + 2 + (25 * numOfRows))}));

                    bmp.Mutate(x => x.DrawText(new DrawingOptions
                    {
                        TextOptions = new TextOptions
                        {
                            HorizontalAlignment = HorizontalAlignment.Center,
                        }
                    }, keyTextStrings[lastHeightchange_i], _smallFont, lastHeightchange_i < 7 ? Color.Black : Color.White, new PointF(bmp.Width / 2, 4 + lastHeightchange_i * 25)));

                    lastHeightchange_i = i;
                    numOfRows = 1;
                    lastKeyString = keyTextStrings[i];
                }
            }

            // Draw the last row
            bmp.Mutate(x => x.FillPolygon(colorKeys[lastHeightchange_i], new PointF[] {
                new PointF(2, (lastHeightchange_i * 25) + 2), new PointF(bmp.Width-2, (lastHeightchange_i * 25) + 2),
                new PointF(bmp.Width - 2, (lastHeightchange_i * 25) + 2 + (25 * numOfRows)), new PointF(2, (lastHeightchange_i * 25) + 2 + (25 * numOfRows))}));

            bmp.Mutate(x => x.DrawText(new DrawingOptions
            {
                TextOptions = new TextOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            }, keyTextStrings[lastHeightchange_i], _smallFont, lastHeightchange_i < 7 ? Color.Black : Color.White, new PointF(bmp.Width / 2, 4+ lastHeightchange_i * 25)));
            
            // Draw the border
            bmp.Mutate(x => x.FillPolygon(Color.Black, new PointF[] {
                new PointF(0,0), new PointF(bmp.Width, 0), new PointF(bmp.Width,2), new PointF(0, 2)}));
            bmp.Mutate(x => x.FillPolygon(Color.Black, new PointF[] {
                new PointF(0,0), new PointF(2, 0), new PointF(2, bmp.Height), new PointF(0, bmp.Height)}));
            bmp.Mutate(x => x.FillPolygon(Color.Black, new PointF[] {
                new PointF(bmp.Width-2, 0), new PointF(bmp.Width, 0), new PointF(bmp.Width, bmp.Height), new PointF(bmp.Width-2, bmp.Height)}));
            bmp.Mutate(x => x.FillPolygon(Color.Black, new PointF[] {
                new PointF(0,bmp.Height-2), new PointF(bmp.Width, bmp.Height-2), new PointF(bmp.Width,bmp.Height), new PointF(0, bmp.Height)}));
            
            return bmp;
        }
        
        private Image<Rgba32> CreateHeader(int cityId, string type)
        {
            string headerStringOne = string.Format("{0} - {1}", Consts.Cities[cityId], type);
            string headerStringTwo = string.Format("{0:MM/dd/yyyy HH:mm:ss zzz}", DateTime.UtcNow);

            Image<Rgba32> bmp = new Image<Rgba32>(Math.Max(headerStringOne.Length, headerStringTwo.Length) * 18, 60);

            bmp.Mutate(x => x.BackgroundColor(Color.White));
            bmp.Mutate(x => x.DrawText(new DrawingOptions
            {
                TextOptions = new TextOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            }, headerStringOne, _largeFont, Color.Black, new PointF(bmp.Width/2, 0)));

            bmp.Mutate(x => x.DrawText(new DrawingOptions
            {
                TextOptions = new TextOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            }, headerStringTwo, _smallFont, Color.Black, new PointF(bmp.Width / 2, 36)));

            return bmp;
        }

        private Image<Rgba32> CreateFooter()
        {
            string footerStringOne = "Generated by the Upland Optimizer Bot";
            string footerStringTwo = "https://discord.gg/hsrxQb7UQb";

            Image<Rgba32> bmp = new Image<Rgba32>(footerStringOne.Length * 12, 50);
            bmp.Mutate(x => x. BackgroundColor(Color.White));

            bmp.Mutate(x => x.DrawText(new DrawingOptions
            {
                TextOptions = new TextOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            }, footerStringOne, _smallFont, Color.Black, new PointF(bmp.Width / 2, 2)));

            bmp.Mutate(x => x.DrawText(new DrawingOptions
            {
                TextOptions = new TextOptions
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            }, footerStringTwo, _smallFont, Color.Black, new PointF(bmp.Width / 2, 27)));

            return bmp;
        }

        private Image<Rgba32> LoadBlankMapByCityId(int cityId)
        {
            string filePath = string.Format("{0}root{0}OptimizerBot{0}CityMaps{0}{1}.bmp", Path.DirectorySeparatorChar, cityId);
            Image<Rgba32> cityMap = (Image<Rgba32>)Image.Load(filePath);
            return cityMap;
        }

        private void BuildStandardKey()
        {
            _standardKey = new List<Color>();
            _standardKey.Add(Color.FromRgb(237, 28, 37));
            _standardKey.Add(Color.FromRgb(242, 101, 33));
            _standardKey.Add(Color.FromRgb(247, 148, 30));
            _standardKey.Add(Color.FromRgb(255, 194, 15));
            _standardKey.Add(Color.FromRgb(255, 242, 0));
            _standardKey.Add(Color.FromRgb(141, 198, 63));
            _standardKey.Add(Color.FromRgb(0, 166, 82));
            _standardKey.Add(Color.FromRgb(0, 170, 157));
            _standardKey.Add(Color.FromRgb(0, 114, 187));
            _standardKey.Add(Color.FromRgb(46, 49, 146));
            _standardKey.Add(Color.FromRgb(148, 44, 97));
        }

        private void BuildColorBlindKey()
        {
            _colorBlindKey = new List<Color>();
            _colorBlindKey.Add(Color.FromRgb(255, 184, 0));
            _colorBlindKey.Add(Color.FromRgb(255, 152, 20));
            _colorBlindKey.Add(Color.FromRgb(253, 120, 39));
            _colorBlindKey.Add(Color.FromRgb(243, 88, 54));
            _colorBlindKey.Add(Color.FromRgb(228, 54, 67));
            _colorBlindKey.Add(Color.FromRgb(207, 7, 79));
            _colorBlindKey.Add(Color.FromRgb(182, 0, 88));
            _colorBlindKey.Add(Color.FromRgb(152, 0, 95));
            _colorBlindKey.Add(Color.FromRgb(118, 0, 99));
            _colorBlindKey.Add(Color.FromRgb(79, 0, 98));
            _colorBlindKey.Add(Color.FromRgb(28, 0, 94));
        }

        private List<Color> BuildCustomKey(List<string> colors)
        {
            List<Color> customKey = new List<Color>();

            foreach (string color in colors)
            {
                string translatedColor = color;
                //Remove # if present
                if (translatedColor.IndexOf('#') != -1)
                {
                    translatedColor = translatedColor.Replace("#", "");
                }

                byte red = 0;
                byte green = 0;
                byte blue = 0;

                if (translatedColor.Length == 6)
                {
                    //#RRGGBB
                    red = byte.Parse(translatedColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                    green = byte.Parse(translatedColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                    blue = byte.Parse(translatedColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                }
                else if (translatedColor.Length == 3)
                {
                    //#RGB
                    red = byte.Parse(translatedColor[0].ToString() + translatedColor[0].ToString(), NumberStyles.AllowHexSpecifier);
                    green = byte.Parse(translatedColor[1].ToString() + translatedColor[1].ToString(), NumberStyles.AllowHexSpecifier);
                    blue = byte.Parse(translatedColor[2].ToString() + translatedColor[2].ToString(), NumberStyles.AllowHexSpecifier);
                }

                customKey.Add(Color.FromRgb(red, green, blue));
            }

            return customKey;
        }
    }
}
