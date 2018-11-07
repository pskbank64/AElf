using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using Google.Protobuf;
using NLog;

namespace AElf.Kernel.Managers
{
    public class TransactionReceiptManager : ITransactionReceiptManager
    {
        private readonly IKeyValueDatabase _database;

        private const string _dbName = "TransactionReceipt";
        private readonly ILogger _logger;

        public TransactionReceiptManager(IKeyValueDatabase database)
        {
            _database = database;
            _logger = LogManager.GetLogger(nameof(TransactionReceiptManager));
        }

        private static string GetKey(Hash txId)
        {
            return $"{GlobalConfig.TransactionReceiptPrefix}{txId.DumpHex()}";
        }

        public async Task AddOrUpdateReceiptAsync(TransactionReceipt receipt)
        {
            string key = GetKey(receipt.TransactionId);
            _logger.Info("[##TransactionReceiptManager]: Type-[{0}], Key-[{1}], Length=[{2}], Value-[{3}]", "TransactionReceipt", key,
                receipt.ToByteArray().Length, receipt);
            await _database.SetAsync(_dbName,key, receipt.ToByteArray());
        }

        public async Task AddOrUpdateReceiptsAsync(IEnumerable<TransactionReceipt> receipts)
        {
            var dict = receipts.ToDictionary(r => GetKey(r.TransactionId), r => r.ToByteArray());
            int count = 0;
            foreach (var item in dict.Keys)
            {
                _logger.Info("[##TransactionReceiptManager]: Type-[{0}], Key-[{1}], Length=[{2}], Value-[{3}]", "TransactionReceipt", item,
                    dict[item].Length, dict[item]);
                count++;
            }
            await _database.PipelineSetAsync(_dbName,dict);
        }

        public async Task<TransactionReceipt> GetReceiptAsync(Hash txId)
        {
            var res = await _database.GetAsync(_dbName,GetKey(txId));
            return res?.Deserialize<TransactionReceipt>();
        }
    }
}