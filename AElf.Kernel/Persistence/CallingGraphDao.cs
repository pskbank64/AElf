using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Persistence
{
    public class CallingGraphDao : ICallingGraphDao
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "CallingGraph";

        public CallingGraphDao(IKeyValueDatabase database)
        {
            _database = database;
        }

        public async Task AddOrUpdateAsync(Hash key, SerializedCallGraph serializedCallGraph)
        {
            await _database.SetAsync(_dbName, key.DumpHex(), serializedCallGraph.ToByteArray());
        }

        public async Task<SerializedCallGraph> GetAsync(Hash key)
        {
            var data = await _database.GetAsync(_dbName, key.DumpHex());
            return data?.Deserialize<SerializedCallGraph>();
        }
    }
}