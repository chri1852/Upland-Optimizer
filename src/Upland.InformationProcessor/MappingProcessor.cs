using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.Types;
using Upland.Types.UplandApiTypes;

namespace Upland.InformationProcessor
{
    public class MappingProcessor
    {
        private LocalDataManager _localDataManager;
        private ProfileAppraiser _profileAppraiser;

        private List<Color> _standardKey;
        private List<Color> _colorBlindKey;

        private readonly List<string> _validTypes;

        public MappingProcessor(LocalDataManager localDataManager, ProfileAppraiser profileAppraiser)
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

        public void SaveMap(Bitmap map, string fileName)
        {
            if (!Directory.Exists(".\\GeneratedMaps"))
            {
                Directory.CreateDirectory(".\\GeneratedMaps");
            }

            map.Save(string.Format(".\\GeneratedMaps\\{0}.bmp", fileName));
        }

        public void DeleteSavedMap(string fileName)
        {
            File.Delete(string.Format(".\\GeneratedMaps\\{0}.bmp", fileName));
        }

        public string GetMapLocaiton(string fileName)
        {
            return string.Format(".\\GeneratedMaps\\{0}.bmp", fileName);
        }

        public string CreateMap(int cityId, string type, int registeredUserId, bool colorBlind)
        {
            string typeString;
            Bitmap map;
            Bitmap key;

            if (type.ToUpper() == "SOLD")
            {
                typeString = "Sold Out";
                map = CreateSoldOutMap(cityId, colorBlind, false);
                key = BuildStandardKey(colorBlind);
            }
            else if (type.ToUpper() == "SOLDNONFSA")
            {
                typeString = "Sold Out Non FSA";
                map = CreateSoldOutMap(cityId, colorBlind, true);
                key = BuildStandardKey(colorBlind);
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

                key = BuildFloorKey(lowestHoodPrice, colorBlind); ;
                map = CreateFloorMap(cityId, lowestHoodPrice, colorBlind);
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

                key = BuildPerUP2Key(neighborhoodPerUp2, colorBlind);
                map = CreatePerUP2Map(cityId, neighborhoodPerUp2, colorBlind);
            }
            else
            {
                throw new Exception("Invalid Map Type Selected");
            }


            Bitmap header = CreateHeader(cityId, typeString);
            Bitmap footer = CreateFooter();

            Bitmap combinedMap = new Bitmap(Math.Max(map.Width + key.Width, header.Width), map.Height + 85);

            using (Graphics g = Graphics.FromImage(combinedMap))
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                {
                    g.FillRectangle(brush, 0, 0, combinedMap.Width, combinedMap.Height);
                }

                g.DrawImage(map, 0, header.Height + 1);
                g.DrawImage(key, map.Width+1, header.Height + 1);

                if (combinedMap.Width > header.Width)
                {
                    g.DrawImage(header, (combinedMap.Width - header.Width) / 2, 0);
                    g.DrawImage(footer, (combinedMap.Width - footer.Width) / 2, combinedMap.Height + -25);
                }
                else
                {
                    g.DrawImage(header, 0, 0);
                    g.DrawImage(footer, 0, combinedMap.Height + -25);
                }
                g.Flush();
            }

            string filename = string.Format("{0}_{1}_{2}", Consts.Cities[cityId], type.ToUpper(), registeredUserId);

            SaveMap(combinedMap, filename);

            return filename;
        }

        private Bitmap CreateFloorMap(int cityId, Dictionary<int, double> lowestHoodPrice, bool colorBlind)
        {
            Bitmap testMap = LoadBlankMapByCityId(cityId);

            List<Color> colorKeys = colorBlind ? _colorBlindKey : _standardKey;

            Dictionary<Color, Neighborhood> colorDictionary = _localDataManager.GetNeighborhoods()
                .Where(n => n.CityId == cityId)
                .ToDictionary(n => Color.FromArgb(n.RGB[0], n.RGB[1], n.RGB[2]), n => n);

            Bitmap newBitmap = new Bitmap(testMap.Width, testMap.Height);

            List<double> formattedOrderedPrices = lowestHoodPrice.Select(d => d.Value).OrderBy(p => p).ToList();
            double numberInbetween = formattedOrderedPrices.Where(m => m != double.MaxValue).ToList().Count / 10.0;
            double max = formattedOrderedPrices.Where(m => m != double.MaxValue).Max();

            Color actualColor;
            int neigborhoodId;
            double price;

            for (int i = 0; i < testMap.Width; i++)
            {
                for (int j = 0; j < testMap.Height; j++)
                {
                    //get the pixel from the scrBitmap image
                    actualColor = testMap.GetPixel(i, j);

                    if (colorDictionary.ContainsKey(actualColor))
                    {
                        neigborhoodId = colorDictionary[actualColor].Id;
                        price = lowestHoodPrice[neigborhoodId];

                        if (price == double.MaxValue)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[10]);
                        }
                        else if (price >= max)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[9]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 9)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[8]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 8)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[7]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 7)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[6]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 6)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[5]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 5)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[4]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 4)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[3]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 3)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[2]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 2)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[1]);
                        }
                        else
                        {
                            newBitmap.SetPixel(i, j, colorKeys[0]);
                        }
                    }
                    else
                    {
                        newBitmap.SetPixel(i, j, actualColor);
                    }
                }
            }

            return newBitmap;
        }

        private Bitmap CreatePerUP2Map(int cityId, Dictionary<int, double> neighborhoodPerUp2, bool colorBlind)
        {
            Bitmap testMap = LoadBlankMapByCityId(cityId);

            List<Color> colorKeys = colorBlind ? _colorBlindKey : _standardKey;

            Dictionary<Color, Neighborhood> colorDictionary = _localDataManager.GetNeighborhoods()
                .Where(n => n.CityId == cityId)
                .ToDictionary(n => Color.FromArgb(n.RGB[0], n.RGB[1], n.RGB[2]), n => n);

            Bitmap newBitmap = new Bitmap(testMap.Width, testMap.Height);

            List<double> formattedOrderedPrices = neighborhoodPerUp2.Select(d => d.Value).OrderBy(p => p).ToList();
            double numberInbetween = formattedOrderedPrices.Where(m => m != double.MaxValue).ToList().Count / 10.0;
            double max = formattedOrderedPrices.Where(m => m != double.MaxValue).Max();

            Color actualColor;
            int neigborhoodId;
            double price;

            for (int i = 0; i < testMap.Width; i++)
            {
                for (int j = 0; j < testMap.Height; j++)
                {
                    //get the pixel from the scrBitmap image
                    actualColor = testMap.GetPixel(i, j);

                    if (colorDictionary.ContainsKey(actualColor))
                    {
                        neigborhoodId = colorDictionary[actualColor].Id;
                        price = neighborhoodPerUp2[neigborhoodId];

                        if (price == double.MaxValue)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[10]);
                        }
                        else if (price >= max)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[9]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 9)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[8]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 8)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[7]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 7)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[6]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 6)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[5]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 5)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[4]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 4)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[3]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 3)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[2]);
                        }
                        else if (price >= formattedOrderedPrices[(int)Math.Floor(numberInbetween * 2)])
                        {
                            newBitmap.SetPixel(i, j, colorKeys[1]);
                        }
                        else
                        {
                            newBitmap.SetPixel(i, j, colorKeys[0]);
                        }
                    }
                    else
                    {
                        newBitmap.SetPixel(i, j, actualColor);
                    }
                }
            }

            return newBitmap;
        }

        private Bitmap CreateSoldOutMap(int cityId, bool colorBlind, bool nonFSAOnly)
        {
            Bitmap testMap = LoadBlankMapByCityId(cityId);

            List<Color> colorKeys = colorBlind ? _colorBlindKey : _standardKey;

            Dictionary<int, CollatedStatsObject> neighborhoodStats = _localDataManager.GetNeighborhoodStats()
                .ToDictionary(n => n.Id, n => n);

            Dictionary<Color, Neighborhood> colorDictionary = _localDataManager.GetNeighborhoods()
                .Where(n => n.CityId == cityId)
                .ToDictionary(n => Color.FromArgb(n.RGB[0], n.RGB[1], n.RGB[2]), n => n);

            Bitmap newBitmap = new Bitmap(testMap.Width, testMap.Height);

            Color actualColor;
            double percentMinted;

            for (int i = 0; i < testMap.Width; i++)
            {
                for (int j = 0; j < testMap.Height; j++)
                {
                    //get the pixel from the scrBitmap image
                    actualColor = testMap.GetPixel(i, j);

                    if (colorDictionary.ContainsKey(actualColor))
                    {
                        percentMinted = nonFSAOnly 
                            ? neighborhoodStats[colorDictionary[actualColor].Id].PercentNonFSAMinted
                            : neighborhoodStats[colorDictionary[actualColor].Id].PercentMinted;

                        if (percentMinted == 100)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[10]);
                        }
                        else if (percentMinted >= 90)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[9]);
                        }
                        else if (percentMinted >= 80)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[8]);
                        }
                        else if (percentMinted >= 70)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[7]);
                        }
                        else if (percentMinted >= 60)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[6]);
                        }
                        else if (percentMinted >= 50)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[5]);
                        }
                        else if (percentMinted >= 40)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[4]);
                        }
                        else if (percentMinted >= 30)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[3]);
                        }
                        else if (percentMinted >= 20)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[2]);
                        }
                        else if (percentMinted >= 10)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[1]);
                        }
                        else
                        {
                            newBitmap.SetPixel(i, j, colorKeys[0]);
                        }
                    }
                    else
                    {
                        newBitmap.SetPixel(i, j, actualColor);
                    }
                }
            }

            return newBitmap;
        }

        private Bitmap BuildFloorKey(Dictionary<int, double> lowestHoodPrice, bool colorBlind)
        {
            List<string> formattedOrderedPrices = lowestHoodPrice
                .Where(l => l.Value != double.MaxValue)
                .GroupBy(l => l.Value)
                .Select(d => d.First().Value)
                .OrderBy(p => p)
                .ToList().Select(p => string.Format("{0:N2}", p)).ToList();
            int maxPriceLength = formattedOrderedPrices.Select(p => string.Format("{0:N2}", p)).Max(v => v.Length);
            double numberInbetween = formattedOrderedPrices.Count / 10.0;

            List<string> keyTextStrings = new List<string>();
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween*2)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween*3)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween*4)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween*5)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween*6)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween*7)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween*8)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween*9)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[formattedOrderedPrices.Count-1]).PadLeft(maxPriceLength));
            keyTextStrings.Add("No Floor");

            return BuildKey(colorBlind, keyTextStrings);
        }

        private Bitmap BuildPerUP2Key(Dictionary<int, double> neighborhoodPerUp2, bool colorBlind)
        {
            List<string> formattedOrderedPrices = neighborhoodPerUp2
                .Where(l => l.Value != double.MaxValue)
                .GroupBy(l => l.Value)
                .Select(d => d.First().Value)
                .OrderBy(p => p)
                .ToList().Select(p => string.Format("{0:N2}", p)).ToList();
            int maxPriceLength = formattedOrderedPrices.Select(p => string.Format("{0:N2}", p)).Max(v => v.Length);
            double numberInbetween = formattedOrderedPrices.Count / 10.0;

            List<string> keyTextStrings = new List<string>();
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 2)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 3)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 4)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 5)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 6)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 7)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 8)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[(int)Math.Floor(numberInbetween * 9)].PadLeft(maxPriceLength)));
            keyTextStrings.Add(string.Format(" < {0}", formattedOrderedPrices[formattedOrderedPrices.Count - 1]).PadLeft(maxPriceLength));
            keyTextStrings.Add("Low Market Data");

            return BuildKey(colorBlind, keyTextStrings);
        }

        private Bitmap BuildStandardKey(bool colorBlind)
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

            return BuildKey(colorBlind, keyTextStrings);
        }

        private Bitmap BuildKey(bool colorBlind, List<string> keyTextStrings)
        {
            List<Color> colorKeys = colorBlind ? _colorBlindKey : _standardKey;

            Bitmap bmp = new Bitmap((keyTextStrings.Max(t => t.Length) * 14) + 4, 279);
            Graphics g = Graphics.FromImage(bmp);
            Font font = new Font("Consolas", 20, GraphicsUnit.Pixel);
            StringFormat format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(0, 0, 0)))
            {
                g.FillRectangle(brush, 0, 0, bmp.Width, 2);
                g.FillRectangle(brush, 0, 0, 2, bmp.Height);
                g.FillRectangle(brush, bmp.Width-2, 0, bmp.Width, bmp.Height);
                g.FillRectangle(brush, 0, bmp.Height-2, bmp.Width, bmp.Height);
            }

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
                    rectf = new RectangleF(2, (lastHeightchange_i * 25) + 2, bmp.Width - 4, 25 * numOfRows);
                    using (SolidBrush brush = new SolidBrush(colorKeys[i - 1]))
                    {
                        g.FillRectangle(brush, rectf);
                    }

                    g.DrawString(keyTextStrings[lastHeightchange_i], font, i-1 < 7 ? Brushes.Black : Brushes.White, rectf, format);

                    lastHeightchange_i = i;
                    numOfRows = 1;
                    lastKeyString = keyTextStrings[i];
                }
            }

            // Draw the last row
            rectf = new RectangleF(2, (lastHeightchange_i * 25) + 2, bmp.Width - 4, 25 * numOfRows);
            using (SolidBrush brush = new SolidBrush(colorKeys[lastHeightchange_i]))
            {
                g.FillRectangle(brush, rectf);
            }

            g.DrawString(keyTextStrings[lastHeightchange_i], font, lastHeightchange_i < 7 ? Brushes.Black : Brushes.White, rectf, format);

            g.Flush();

            return bmp;
        }

        private Bitmap CreateHeader(int cityId, string type)
        {
            string headerStringOne = string.Format("{0} - {1}", Consts.Cities[cityId], type);
            string headerStringTwo = string.Format("{0:MM/dd/yyyy HH:mm:ss zzz}", DateTime.UtcNow);

            Bitmap bmp = new Bitmap(Math.Max(headerStringOne.Length, headerStringTwo.Length) * 18, 60);

            RectangleF rectf;

            // Create graphic object that will draw onto the bitmap
            Graphics g = Graphics.FromImage(bmp);

            StringFormat format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            {
                g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
            }

            rectf = new RectangleF(0, 0, bmp.Width, 35);
            g.DrawString(headerStringOne, new Font("Consolas", 30, GraphicsUnit.Pixel), Brushes.Black, rectf, format);

            rectf = new RectangleF(0, 36, bmp.Width, 25);
            g.DrawString(headerStringTwo, new Font("Consolas", 20, GraphicsUnit.Pixel), Brushes.Black, rectf, format);

            g.Flush();

            return bmp;
        }

        private Bitmap CreateFooter()
        {
            string footerString = "Generated by the Upland Optimizer Bot - https://discord.gg/hsrxQb7UQb";

            Bitmap bmp = new Bitmap(footerString.Length * 12, 25);

            RectangleF rectf = new RectangleF(0, 0, bmp.Width, bmp.Height);

            // Create graphic object that will draw onto the bitmap
            Graphics g = Graphics.FromImage(bmp);

            StringFormat format = new StringFormat()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
            {
                g.FillRectangle(brush, 0, 0, bmp.Width, bmp.Height);
            }

            g.DrawString(footerString, new Font("Consolas", 20, GraphicsUnit.Pixel), Brushes.Black, rectf, format);

            g.Flush();

            return bmp;
        }

        private Bitmap LoadBlankMapByCityId(int cityId)
        {
            Bitmap cityMap = new Bitmap(string.Format(".\\CityMaps\\{0}.bmp", cityId));

            return cityMap;
        }

        private void BuildStandardKey()
        {
            _standardKey = new List<Color>();
            _standardKey.Add(Color.FromArgb(237, 28, 37));
            _standardKey.Add(Color.FromArgb(242, 101, 33));
            _standardKey.Add(Color.FromArgb(247, 148, 30));
            _standardKey.Add(Color.FromArgb(255, 194, 15));
            _standardKey.Add(Color.FromArgb(255, 242, 0));
            _standardKey.Add(Color.FromArgb(141, 198, 63));
            _standardKey.Add(Color.FromArgb(0, 166, 82));
            _standardKey.Add(Color.FromArgb(0, 170, 157));
            _standardKey.Add(Color.FromArgb(0, 114, 187));
            _standardKey.Add(Color.FromArgb(46, 49, 146));
            _standardKey.Add(Color.FromArgb(148, 44, 97));
        }

        private void BuildColorBlindKey()
        {
            _colorBlindKey = new List<Color>();
            _colorBlindKey.Add(Color.FromArgb(255, 184, 0));
            _colorBlindKey.Add(Color.FromArgb(255, 152, 20));
            _colorBlindKey.Add(Color.FromArgb(253, 120, 39));
            _colorBlindKey.Add(Color.FromArgb(243, 88, 54));
            _colorBlindKey.Add(Color.FromArgb(228, 54, 67));
            _colorBlindKey.Add(Color.FromArgb(207, 7, 79));
            _colorBlindKey.Add(Color.FromArgb(182, 0, 88));
            _colorBlindKey.Add(Color.FromArgb(152, 0, 95));
            _colorBlindKey.Add(Color.FromArgb(118, 0, 99));
            _colorBlindKey.Add(Color.FromArgb(79, 0, 98));
            _colorBlindKey.Add(Color.FromArgb(28, 0, 94));
        }
    }
}
