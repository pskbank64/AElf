using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Persistence
{
    public class SmartContractStore : ISmartContractStore
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "Block";

        public SmartContractStore(IKeyValueDatabase database)
        {
            _database = database;
        }

        public async Task<SmartContractRegistration> GetAsync(Address contractAddress)
        {
            var data = await _database.GetAsync(_dbName, Hash.FromMessage(contractAddress).DumpHex());
            return data?.Deserialize<SmartContractRegistration>();
        }

        public async Task InsertAsync(Address contractAddress, SmartContractRegistration reg)
        {
            await _database.SetAsync(_dbName, Hash.FromMessage(contractAddress).DumpHex(), reg.ToByteArray());
        }
    }
}