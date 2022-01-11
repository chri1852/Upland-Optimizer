using System.Collections.Generic;
using System.Threading.Tasks;

namespace Upland.Interfaces.Processors
{
    public interface IProfileAppraiser
    {
        Dictionary<int, double> GetNeighborhoodPricePerUP2();
        Task<List<string>> RunAppraisal(string username, string fileType);
    }
}