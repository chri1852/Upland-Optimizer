﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Linq;
using Upland.Infrastructure.LocalData;
using Upland.Types;
using Upland.Types.Types;

namespace Upland.InformationProcessor
{
    public class MappingProcessor
    {
        private LocalDataManager _localDataManager;

        private List<Color> _standardKey;
        private List<Color> _colorBlindKey;

        public MappingProcessor(LocalDataManager localDataManager)
        {
            _localDataManager = localDataManager;

            BuildStandardKey();
            BuildColorBlindKey();
        }

        public Bitmap CreateMap(int cityId, string type, bool colorBlind)
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
            else
            {
                throw new Exception("Invalid Map Type Selected");
            }

            Bitmap combinedMap = new Bitmap(map.Width + key.Width, map.Height + 60);

            Bitmap header = CreateHeader(cityId, typeString, combinedMap.Width);
            Bitmap footer = CreateFooter(combinedMap.Width);

            using (Graphics g = Graphics.FromImage(combinedMap))
            {
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(255, 255, 255)))
                {
                    g.FillRectangle(brush, 0, 0, combinedMap.Width, combinedMap.Height);
                }
                g.DrawImage(header, 0, 0);
                g.DrawImage(map, 0, header.Height + 1);
                g.DrawImage(key, map.Width+1, header.Height + 1);
                g.DrawImage(footer, 0, combinedMap.Height + - 25);
                g.Flush();
            }

            return combinedMap;
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

        private Bitmap BuildStandardKey(bool colorBlind)
        {
            List<string> keyTextStrings = new List<string>();
            keyTextStrings.Add("     > 10%");
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

            for (int i = 0; i < colorKeys.Count; i++)
            {
                RectangleF rectf = new RectangleF(2, (i * 25) + 2, bmp.Width - 4, 25);
                using (SolidBrush brush = new SolidBrush(colorKeys[i]))
                {
                    g.FillRectangle(brush, rectf);
                }

                g.DrawString(keyTextStrings[i], font, i < 7 ? Brushes.Black : Brushes.White, rectf, format);
            }

            g.Flush();

            return bmp;
        }

        private Bitmap CreateHeader(int cityId, string type, int width)
        {
            string headerString = Consts.Cities[cityId];
            headerString += " - ";
            headerString += type;
            headerString += " - ";
            headerString += string.Format("{0:MM/dd/yyyy HH:mm:ss zzz}", DateTime.UtcNow);

            Bitmap bmp = new Bitmap(width, 35);

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

            g.DrawString(headerString, new Font("Consolas", 30, GraphicsUnit.Pixel), Brushes.Black, rectf, format);

            g.Flush();

            return bmp;
        }

        private Bitmap CreateFooter(int width)
        {
            string footerString = "Autogenerated by the Upland Optimizer Bot - https://discord.gg/hsrxQb7UQb";

            Bitmap bmp = new Bitmap(width, 25);

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

        private void SaveMap(Bitmap map, string fileName)
        {
            map.Save(string.Format("C:\\Users\\chri1\\Desktop\\{0}.bmp", fileName));
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
