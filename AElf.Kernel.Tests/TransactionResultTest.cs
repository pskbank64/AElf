using System;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.ChainController;
using AElf.ChainController.Rpc;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Miner.TxMemPool;
using NLog;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class TransactionResultTest
    {
        private readonly ITransactionResultService _transactionResultService;
        private readonly ITransactionResultDao _transactionResultDao;
        private readonly ITxSignatureVerifier _signatureVerifier;
        private readonly ITxRefBlockValidator _refBlockValidator;
        private readonly ITxHub _txHub;

        public TransactionResultTest(ITxPoolConfig txPoolConfig, IChainService chainService,
            ITxSignatureVerifier signatureVerifier, ITxRefBlockValidator refBlockValidator,
            ITransactionResultDao transactionResultDao, ITxHub txHub)
        {
            ChainConfig.Instance.ChainId = Hash.Generate().DumpHex();
            NodeConfig.Instance.NodeAccount = Address.Generate().DumpHex();
            _transactionResultDao = transactionResultDao;
            _signatureVerifier = signatureVerifier;
            _refBlockValidator = refBlockValidator;
            _txHub = txHub;
//            _transactionResultService = new TransactionResultService(
//                new TxPool(logger,
//                    new NewTxHub(transactionManager, chainService, signatureVerifier, refBlockValidator)), transactionResultManager);
            _transactionResultService = new TransactionResultService(_txHub, _transactionResultDao);
        }

        private TransactionResult CreateResult(Hash txId, Status status)
        {
            return new TransactionResult
            {
                TransactionId = txId,
                Status = status
            };
        }

        
        [Fact]
        public async Task TxResultStorage()
        {
            var txId = Hash.Generate();
            var res = CreateResult(txId, Status.Mined);
            await _transactionResultDao.AddTransactionResultAsync(res);
            var r = await _transactionResultDao.GetTransactionResultAsync(txId);
            Assert.Equal(res, r);
        }
        
        [Fact]
        public async Task AddTxResult()
        {
            var txId = Hash.Generate();
            var res = new TransactionResult
            {
                TransactionId = txId,
                Status = Status.Mined
            };
            await _transactionResultService.AddResultAsync(res);
        }
        
        [Fact]
        public async Task GetTxResultNotExisted()
        {
            var txId = Hash.Generate();
            var res = await _transactionResultService.GetResultAsync(txId);
            Assert.Equal(txId, res.TransactionId);
            Assert.Equal(res.Status, Status.NotExisted);
        }

        [Fact]
        public async Task GetTxResultInStorage()
        {
            var txId = Hash.Generate();
            var res = CreateResult(txId, Status.Mined);
            await _transactionResultDao.AddTransactionResultAsync(res);

            var r = await _transactionResultService.GetResultAsync(txId);
            Assert.Equal(res, r);
        }

    }
}