using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Persistence
{
    public interface IFunctionMetadataDao
    {
        Task AddOrUpdateAsync(Hash key, FunctionMetadata functionMetadata);

        Task<FunctionMetadata> GetAsync(Hash key);
    }
}