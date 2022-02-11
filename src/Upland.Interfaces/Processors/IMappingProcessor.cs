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
        string GetDisplayMapType(string mapType);
        string SearchForMap(int cityId, string mapType, int registeredUserId);
        DateTime GetDateFromFileName(string fileName);
        void DeleteSavedMap(string fileName);
        string CreateMap(int cityId, string type, int registeredUserId, bool colorBlind, List<string> customColors);
    }
}