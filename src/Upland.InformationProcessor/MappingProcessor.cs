using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Upland.Infrastructure.LocalData;
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

        public void CreateSoldOutMap(int cityId, bool colorBlind, bool nonFSAOnly)
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

                        if (percentMinted >= 0 && percentMinted < 10)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[0]);
                        }
                        else if (percentMinted >= 10 && percentMinted < 20)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[1]);
                        }
                        else if (percentMinted >= 20 && percentMinted < 30)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[2]);
                        }
                        else if (percentMinted >= 30 && percentMinted < 40)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[3]);
                        }
                        else if (percentMinted >= 40 && percentMinted < 50)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[4]);
                        }
                        else if (percentMinted >= 50 && percentMinted < 60)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[5]);
                        }
                        else if (percentMinted >= 60 && percentMinted < 70)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[6]);
                        }
                        else if (percentMinted >= 70 && percentMinted < 80)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[7]);
                        }
                        else if (percentMinted >= 80 && percentMinted < 90)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[8]); ;
                        }
                        else if (percentMinted >= 90 && percentMinted < 100)
                        {
                            newBitmap.SetPixel(i, j, colorKeys[9]);
                        }
                        else
                        {
                            newBitmap.SetPixel(i, j, colorKeys[10]);
                        }
                    }
                    else
                    {
                        newBitmap.SetPixel(i, j, actualColor);
                    }
                }
            }

            // TODO THIS SHOULD RETURN A BITMAP
            SaveMap(newBitmap, "test123");
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
