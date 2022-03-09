using System.Collections.Generic;
using System.Threading.Tasks;
using Upland.Types.Types;

namespace Upland.Interfaces.Processors
{
    public interface IProfileAppraiser
    {
        Dictionary<int, double> GetNeighborhoodPricePerUP2();
        Dictionary<int, double> GetNeighborhoodMarkupFloor();
        Task<AppraisalResults> RunAppraisal(RegisteredUser registeredUser);
        List<string> BuildAppraisalTxtStrings(AppraisalResults results);
        List<string> BuildAppraisalCsvStrings(AppraisalResults results);
    }
}