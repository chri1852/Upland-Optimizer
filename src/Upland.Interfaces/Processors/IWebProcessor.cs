using System.Threading.Tasks;
using Upland.Types.Types;

namespace Upland.Interfaces.Processors
{
    public interface IWebProcessor
    {
        Task<UserProfile> GetWebUIProfile(string uplandUsername);
    }
}