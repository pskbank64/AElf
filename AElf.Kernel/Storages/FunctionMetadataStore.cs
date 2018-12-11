using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Persistence
{
    public class FunctionMetadataStore : IFunctionMetadataStore
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "FunctionMetadata";

        public FunctionMetadataStore(IKeyValueDatabase database)
        {
            _database = database;
        }

        public async Task AddOrUpdateAsync(Hash key, FunctionMetadata functionMetadata)
        {
            await _database.SetAsync(_dbName, key.DumpHex(), functionMetadata.ToByteArray());
        }

        public async Task<FunctionMetadata> GetAsync(Hash key)
        {
            var data = await _database.GetAsync(_dbName, key.DumpHex());
            return data?.Deserialize<FunctionMetadata>();
        }
    }
}