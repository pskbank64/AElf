using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;
using NLog.Targets.Wrappers;

namespace AElf.Kernel.Persistence
{
    public class LightChainCanonicalStore : ILightChainCanonicalStore
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "LightChainCanonical";
        
        public LightChainCanonicalStore(IKeyValueDatabase database)
        {
            _database = database;
        }

        public async Task AddOrUpdateAsync(Hash key, Hash Canonical)
        {
            await _database.SetAsync(_dbName, key.DumpHex(), Canonical.ToByteArray());
        }

        public async Task<Hash> GetAsync(Hash key)
        {
            var data = await _database.GetAsync(_dbName, key.DumpHex());
            return data?.Deserialize<Hash>();
        }

        public async Task RemoveAsync(Hash key)
        {
            await _database.RemoveAsync(_dbName, key.DumpHex());
        }
    }
}