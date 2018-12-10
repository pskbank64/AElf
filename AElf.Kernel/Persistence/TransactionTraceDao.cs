using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Managers
{
    public class TransactionTraceDao : ITransactionTraceDao
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "TransactionTrace";

        private readonly Hash _typeIdHash = Hash.FromString("__TransactionTrace__");
        
        public TransactionTraceDao(IKeyValueDatabase database)
        {
            _database = database;
        }

        private Hash GetDisambiguatedHash(Hash txId, Hash disambiguationHash)
        {
            var hash = disambiguationHash == null ? txId : Hash.Xor(disambiguationHash, txId);
            return Hash.Xor(hash, _typeIdHash);
        }
        
        public async Task AddTransactionTraceAsync(TransactionTrace tr, Hash disambiguationHash = null)
        {
            var trKey = GetDisambiguatedHash(tr.TransactionId, disambiguationHash);
            await _database.SetAsync(_dbName, trKey.DumpHex(), tr.ToByteArray());
        }

        public async Task<TransactionTrace> GetTransactionTraceAsync(Hash txId, Hash disambiguationHash = null)
        {
            var trKey = GetDisambiguatedHash(txId, disambiguationHash);
            var data = await _database.GetAsync(_dbName, trKey.DumpHex());
            return data?.Deserialize<TransactionTrace>();
        }
    }
}