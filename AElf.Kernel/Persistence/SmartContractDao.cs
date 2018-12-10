using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using AElf.Common;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Managers
{
    public class SmartContractDao : ISmartContractDao
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "Block";

        public SmartContractDao(IKeyValueDatabase database)
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