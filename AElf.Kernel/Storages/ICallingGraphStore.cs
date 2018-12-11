using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Persistence
{
    public interface ICallingGraphStore
    {
        Task AddOrUpdateAsync(Hash key, SerializedCallGraph serializedCallGraph);

        Task<SerializedCallGraph> GetAsync(Hash key);
    }
}