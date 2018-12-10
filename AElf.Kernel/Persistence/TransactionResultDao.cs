using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using AElf.Common;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Managers
{
    public class TransactionResultDao : ITransactionResultDao
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "TransactionResult";
        
        public TransactionResultDao(IKeyValueDatabase database)
        {
            _database = database;
        }

        public async Task AddTransactionResultAsync(TransactionResult tr)
        {
            var trKey = DataPath.CalculatePointerForTxResult(tr.TransactionId);
            await _database.SetAsync(_dbName, trKey.DumpHex(), tr.ToByteArray());
        }

        public async Task<TransactionResult> GetTransactionResultAsync(Hash txId)
        {
            var trKey = DataPath.CalculatePointerForTxResult(txId);
            var data = await _database.GetAsync(_dbName, trKey.DumpHex());
            return data?.Deserialize<TransactionResult>();
        }
    }
}