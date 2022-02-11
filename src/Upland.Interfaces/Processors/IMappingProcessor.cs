using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Text;

namespace Upland.Interfaces.Processors
{
    public interface IMappingProcessor
    {
        bool IsValidType(string type);
        List<string> GetValidTypes();
        void SaveMap(Image<Rgba32> map, string fileName);
        string SearchForMap(int cityId, string mapType, int registeredUserId);
        DateTime GetDateFromFileName(string fileName);
        void DeleteSavedMap(string fileName);
        string GetMapLocaiton(string fileName);
        string CreateMap(int cityId, string type, int registeredUserId, bool colorBlind, List<string> customColors);
    }
}