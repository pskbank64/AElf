using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Persistence;
using AElf.Miner.TxMemPool;

namespace AElf.ChainController.Rpc
{
    public class TransactionResultService : ITransactionResultService
    {
        private readonly ITransactionResultDao _transactionResultDao;
        private readonly ITxHub _txHub;
        private readonly Dictionary<Hash, TransactionResult> _cacheResults = new Dictionary<Hash, TransactionResult>();

        public TransactionResultService(ITxHub txHub, ITransactionResultDao transactionResultDao)
        {
            _txHub = txHub;
            _transactionResultDao = transactionResultDao;
        }

        /// <inheritdoc/>
        public async Task<TransactionResult> GetResultAsync(Hash txId)
        {
            /*// found in cache
            if (_cacheResults.TryGetValue(txId, out var res))
            {
                return res;
            }*/

            
            // in storage
            var res = await _transactionResultDao.GetTransactionResultAsync(txId);
            if (res != null)
            {
                _cacheResults[txId] = res;
                return res;
            }

            // in tx pool
            if (_txHub.TryGetTx(txId, out var tx))
            {
                return new TransactionResult
                {
                    TransactionId = tx.GetHash(),
                    Status = Status.Pending
                };
            }
            
            // not existed
            return new TransactionResult
            {
                TransactionId = txId,
                Status = Status.NotExisted
            };
        }

        /// <inheritdoc/>
        public async Task AddResultAsync(TransactionResult res)
        {
            _cacheResults[res.TransactionId] = res;
            await _transactionResultDao.AddTransactionResultAsync(res);
        }
    }
}