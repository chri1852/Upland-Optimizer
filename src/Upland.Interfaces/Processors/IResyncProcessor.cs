using System.Threading.Tasks;

namespace Upland.Interfaces.Processors
{
    public interface IResyncProcessor
    {
        Task ResyncPropsList(string action, string propList);
    }
}
