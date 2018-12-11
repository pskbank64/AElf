using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.Database;
using AElf.Execution.Execution;
using AElf.Kernel;
using AElf.Kernel.Persistence;
using AElf.Miner.TxMemPool;
using AElf.SmartContract;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;
using Moq;

namespace AElf.Synchronization.Tests
{
    public class MockSetup
    {
        private List<IBlockHeader> _headers = new List<IBlockHeader>();
        private List<IBlockHeader> _sideChainHeaders = new List<IBlockHeader>();
        private List<IBlock> _blocks = new List<IBlock>();
        
        private readonly IStateDao _stateDao;
        private readonly ISmartContractDao _smartContractDao;
        private readonly IKeyValueDatabase _database;
        private ITransactionDao _transactionDao;
        private ITransactionResultDao _transactionResultDao;
        private ITransactionTraceDao _transactionTraceDao;
        private ISmartContractRunnerFactory _smartContractRunnerFactory;
        private IFunctionMetadataService _functionMetadataService;
        private IExecutingService _concurrencyExecutingService;
        private ITxHub _txHub;
        private IChainDao _chainDao;

        private IBlockSynchronizer _blockSynchronizer;

        public MockSetup(IStateDao stateDao, ITxHub txHub, IKeyValueDatabase database)
        {
            _stateDao = stateDao;
            _database = database;
            
            _smartContractDao = new SmartContractDao(_database);
            _transactionDao = new TransactionDao(_database);
            _transactionTraceDao = new TransactionTraceDao(_database);
            _transactionResultDao = new TransactionResultDao(_database);
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            _concurrencyExecutingService = new SimpleExecutingService(
                new SmartContractService(_smartContractDao, _smartContractRunnerFactory, _stateDao,
                    _functionMetadataService), _transactionTraceDao, _stateDao,
                new ChainContextService(GetChainService()));
            _txHub = txHub;
            _chainDao = new ChainDao(_database);
        }

        public IBlockSynchronizer GetBlockSynchronizer()
        {
            var executor = GetBlockExecutor();
            return new BlockSynchronizer(GetChainService(), GetBlockValidationService(), executor,
                new BlockSet(), null);
        }

        public IChainService GetChainService()
        {
            Mock<IChainService> mock = new Mock<IChainService>();
            mock.Setup(cs => cs.GetLightChain(It.IsAny<Hash>())).Returns(MockLightChain().Object);
            mock.Setup(cs => cs.GetBlockChain(It.IsAny<Hash>())).Returns(MockBlockChain().Object);
            return mock.Object;
        }

        private Mock<ILightChain> MockLightChain()
        {
            Mock<ILightChain> mock = new Mock<ILightChain>();
            mock.Setup(lc => lc.GetCurrentBlockHeightAsync())
                .Returns(Task.FromResult((ulong) _headers.Count - 1 + GlobalConfig.GenesisBlockHeight));
            mock.Setup(lc => lc.GetHeaderByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(_sideChainHeaders[(int) p - 1]));

            return mock;
        }

        private Mock<IBlockChain> MockBlockChain()
        {
            Mock<IBlockChain> mock = new Mock<IBlockChain>();
            mock.Setup(bc => bc.GetBlockByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(_blocks[(int) p - 1]));
            return mock;
        }
        
        /// <summary>
        /// Which will always return true.
        /// </summary>
        /// <returns></returns>
        public IBlockValidationService GetBlockValidationService()
        {
            var mock = new Mock<IBlockValidationService>();
            mock.Setup(bvs => bvs.ValidateBlockAsync(It.IsAny<IBlock>(), It.IsAny<IChainContext>()))
                .Returns(() => Task.FromResult(BlockValidationResult.Success));
            return mock.Object;
        }

        public IBlockExecutor GetBlockExecutor()
        {
            return new BlockExecutor(GetChainService(), _concurrencyExecutingService, 
                _transactionResultDao, null, null, _txHub, _chainDao,_stateDao);
        }
    }
}