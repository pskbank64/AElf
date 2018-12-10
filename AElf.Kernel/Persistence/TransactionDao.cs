using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using NLog;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Managers
{
    public class TransactionDao: ITransactionDao
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "Transaction";

        public TransactionDao(IKeyValueDatabase database)
        {
            _database = database;
        }

        public async Task<Hash> AddTransactionAsync(Transaction tx)
        {
            await _database.SetAsync(_dbName, tx.GetHash().DumpHex(), tx.ToByteArray());
            return tx.GetHash();
        }

        public async Task<Transaction> GetTransaction(Hash txId)
        {
            var data = await _database.GetAsync(_dbName, txId.DumpHex());
            return data?.Deserialize<Transaction>();
        }

        public async Task RemoveTransaction(Hash txId)
        {
            await _database.RemoveAsync(_dbName, txId.DumpHex());
        }
        
//        public async Task<List<Transaction>> RollbackTransactions(Hash chainId, ulong currentHeight, ulong specificHeight)
//        {
//            var txs = new List<Transaction>();
//            for (var i = currentHeight - 1; i >= specificHeight; i--)
//            {
//                var rollBackBlockHash =
//                    await _dataStore.GetAsync<Hash>(
//                        DataPath.CalculatePointerForGettingBlockHashByHeight(chainId, i));
//                var header = await _dataStore.GetAsync<BlockHeader>(rollBackBlockHash);
//                var body = await _dataStore.GetAsync<BlockBody>(
//                    Hash.Xor(
//                    header.GetHash(),header.MerkleTreeRootOfTransactions));
//                foreach (var txId in body.Transactions)
//                {
//                    var tx = await _dataStore.GetAsync<Transaction>(txId);
//                    if (tx == null)
//                    {
//                        _logger?.Trace($"tx {txId} is null.");
//                    }
//                    txs.Add(tx);
//                    await _dataStore.RemoveAsync<Transaction>(txId);
//                }
//
//                _logger?.Trace($"Rollback block hash: {rollBackBlockHash.Value.ToByteArray().ToHex()}");
//            }
//
//            return txs;
//        }
    }
}