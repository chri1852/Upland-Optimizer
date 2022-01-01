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

        public void TEST_NASHVILLE_SELLOUT_COLOR()
        {
            Bitmap testMap = LoadBlankMapByCityId(10016);

            List<CollatedStatsObject> neighborhoodStats = _localDataManager.GetNeighborhoodStats();
            List<Neighborhood> neighborhoods = _localDataManager.GetNeighborhoods().Where(n => n.CityId == 16).OrderBy(n => n.Id).ToList();

            /*
            List<Color> hoodColors = new List<Color>();
            hoodColors.Add(Color.FromArgb(16, 8, 255));
            hoodColors.Add(Color.FromArgb(16, 9, 255));
            hoodColors.Add(Color.FromArgb(16, 4, 255));
            hoodColors.Add(Color.FromArgb(16, 12, 255));
            hoodColors.Add(Color.FromArgb(16, 10, 255));
            hoodColors.Add(Color.FromArgb(16, 1, 255));
            hoodColors.Add(Color.FromArgb(16, 3, 255));
            hoodColors.Add(Color.FromArgb(16, 7, 255));
            hoodColors.Add(Color.FromArgb(16, 13, 255));
            hoodColors.Add(Color.FromArgb(16, 2, 255));
            hoodColors.Add(Color.FromArgb(16, 5, 255));
            hoodColors.Add(Color.FromArgb(16, 6, 255));
            hoodColors.Add(Color.FromArgb(16, 11, 255));
            hoodColors.Add(Color.FromArgb(16, 14, 255));
            */
            ColorPalette palette = testMap.Palette;

            foreach (Neighborhood neighborhood in neighborhoods)
            {
                double percentMinted = neighborhoodStats.First(n => n.Id == neighborhood.Id).PercentMinted;

                for (int i = 0; i < testMap.Palette.Entries.Length; i++)
                {
                    if (testMap.Palette.Entries[i] == Color.FromArgb(neighborhood.RGB[0], neighborhood.RGB[1], neighborhood.RGB[2]))
                    {
                        if (percentMinted >= 0 && percentMinted < 10)
                        {
                            palette.Entries[i] = _standardKey[0];
                        }
                        else if (percentMinted >= 10 && percentMinted < 20)
                        {
                            palette.Entries[i] = _standardKey[1];
                        }
                        else if (percentMinted >= 20 && percentMinted < 30)
                        {
                            palette.Entries[i] = _standardKey[2];
                        }
                        else if (percentMinted >= 30 && percentMinted < 40)
                        {
                            palette.Entries[i] = _standardKey[3];
                        }
                        else if (percentMinted >= 40 && percentMinted < 50)
                        {
                            palette.Entries[i] = _standardKey[4];
                        }
                        else if (percentMinted >= 50 && percentMinted < 60)
                        {
                            palette.Entries[i] = _standardKey[5];
                        }
                        else if (percentMinted >= 60 && percentMinted < 70)
                        {
                            palette.Entries[i] = _standardKey[6];
                        }
                        else if (percentMinted >= 70 && percentMinted < 80)
                        {
                            palette.Entries[i] = _standardKey[7];
                        }
                        else if (percentMinted >= 80 && percentMinted < 90)
                        {
                            palette.Entries[i] = _standardKey[8];
                        }
                        else if (percentMinted >= 90 && percentMinted < 100)
                        {
                            palette.Entries[i] = _standardKey[9];
                        }
                        else
                        {
                            palette.Entries[i] = _standardKey[10];
                        }
                    }
                }
            }

            testMap.Palette = palette;

            SaveMap(testMap, "test123");
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
