using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Persistence
{
    public interface ILightChainCanonicalStore
    {
        Task AddOrUpdateAsync(Hash key, Hash Canonical);

        Task<Hash> GetAsync(Hash key);

        Task RemoveAsync(Hash key);
    }
}