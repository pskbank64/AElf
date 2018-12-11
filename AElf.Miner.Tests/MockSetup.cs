using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.CrossChain;
using AElf.Configuration;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Miner.Miner;
using AElf.Miner.Rpc.Server;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using AElf.SmartContract.Metadata;
using Google.Protobuf;
using Moq;
using NLog;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Database;
using AElf.Execution.Execution;
using AElf.Kernel.Persistence;
using AElf.Miner.Rpc.Client;
using AElf.Miner.TxMemPool;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;

namespace AElf.Miner.Tests
{
    public class MockSetup
    {
        private List<IBlockHeader> _headers = new List<IBlockHeader>();
        private List<IBlockHeader> _sideChainHeaders = new List<IBlockHeader>();
        private List<IBlock> _blocks = new List<IBlock>();
        private readonly ILogger _logger;
        private ulong _i = 0;
        private IChainCreationService _chainCreationService;
        private ISmartContractDao _smartContractDao;
        private ISmartContractRunnerFactory _smartContractRunnerFactory;
        private ITransactionDao _transactionDao;
        private ITransactionReceiptDao _transactionReceiptDao;
        private ITransactionResultDao _transactionResultDao;
        private ITransactionTraceDao _transactionTraceDao;
        private IExecutingService _concurrencyExecutingService;
        private IFunctionMetadataService _functionMetadataService;
        private IChainService _chainService;
        private IBinaryMerkleTreeDao _binaryMerkleTreeDao;
        private IKeyValueDatabase _database;
        private readonly IDataStore _dataStore;
        public readonly IStateDao StateDao;
        private IChainContextService _chainContextService;
        private ITxSignatureVerifier _signatureVerifier;
        private ITxRefBlockValidator _refBlockValidator;
        private IChainDao _chainDao;
        private IStateDao _stateDao;
        private ILightChainCanonicalDao _lightChainCanonicalDao;

        public MockSetup(ILogger logger, IKeyValueDatabase database, IDataStore dataStore, IStateDao stateDao, ITxSignatureVerifier signatureVerifier, ITxRefBlockValidator refBlockValidator)
        {
            _logger = logger;
            _database = database;
            _dataStore = dataStore;
            StateDao = stateDao;
            _signatureVerifier = signatureVerifier;
            _refBlockValidator = refBlockValidator;
            Initialize();
        }

        private void Initialize()
        {
            _transactionDao = new TransactionDao(_database);
            _transactionReceiptDao = new TransactionReceiptDao(_database);
            _smartContractDao = new SmartContractDao(_database);
            _transactionResultDao = new TransactionResultDao(_database);
            _transactionTraceDao = new TransactionTraceDao(_database);
            _functionMetadataService = new FunctionMetadataService(_dataStore, _logger);
            _chainDao = new ChainDao(_database);
            _lightChainCanonicalDao = new LightChainCanonicalDao(_database);
            _chainService = new ChainService(_chainDao, new BlockDao(_database),
                _transactionDao, _transactionTraceDao, _dataStore, StateDao, _lightChainCanonicalDao);
            _smartContractRunnerFactory = new SmartContractRunnerFactory();
            /*var runner = new SmartContractRunner("../../../../AElf.SDK.CSharp/bin/Debug/netstandard2.0/");
            _smartContractRunnerFactory.AddRunner(0, runner);*/
            var runner = new SmartContractRunner(ContractCodes.TestContractFolder);
            _smartContractRunnerFactory.AddRunner(0, runner);
            _concurrencyExecutingService = new SimpleExecutingService(
                new SmartContractService(_smartContractDao, _smartContractRunnerFactory, StateDao,
                    _functionMetadataService), _transactionTraceDao, StateDao,
                new ChainContextService(_chainService));
            
            _chainCreationService = new ChainCreationService(_chainService,
                new SmartContractService(new SmartContractDao(_database), _smartContractRunnerFactory,
                    StateDao, _functionMetadataService), _logger);

            _binaryMerkleTreeDao = new BinaryMerkleTreeDao(_database);
            _chainContextService = new ChainContextService(_chainService);
            _stateDao = new StateDao(_database);
        }

        private byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;

        public async Task<IChain> CreateChain()
        {            
            var chainId = Hash.Generate();
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.FromRawBytes(SmartContractZeroCode)
            };

            var chain = await _chainCreationService.CreateNewChainAsync(chainId,
                new List<SmartContractRegistration> {reg});
            return chain;
        }

        internal IMiner GetMiner(IMinerConfig config, ITxHub hub, ClientManager clientManager = null)
        {
            var miner = new AElf.Miner.Miner.Miner(config, hub, _chainService, _concurrencyExecutingService,
                _transactionResultDao, _logger, clientManager, _binaryMerkleTreeDao, null,
                MockBlockValidationService().Object, _chainContextService, _chainDao, _stateDao);

            return miner;
        }

        internal IBlockExecutor GetBlockExecutor(ClientManager clientManager = null)
        {
            var blockExecutor = new BlockExecutor(_chainService, _concurrencyExecutingService,
                _transactionResultDao, clientManager, _binaryMerkleTreeDao,
                new TxHub(_transactionDao, _transactionReceiptDao, _chainService, _signatureVerifier, _refBlockValidator), _chainDao, StateDao);

            return blockExecutor;
        }

        internal IBlockChain GetBlockChain(Hash chainId)
        {
            return _chainService.GetBlockChain(chainId);
        }
        
        internal ITxHub CreateTxPool()
        {
            var validator = new TxValidator(TxPoolConfig.Default, _chainService, _logger);
            return new TxHub(_transactionDao, _transactionReceiptDao, _chainService, _signatureVerifier, _refBlockValidator);
//            return new TxPool(_logger, new NewTxHub(_transactionManager, _chainService, _signatureVerifier, _refBlockValidator));
        }

        public IMinerConfig GetMinerConfig(Hash chainId, ulong txCountLimit, byte[] getAddress)
        {
            return new MinerConfig
            {
                ChainId = chainId,
                CoinBase = Address.FromRawBytes(getAddress)
            };
        }

        private Mock<ILightChain> MockLightChain()
        {
            Mock<ILightChain> mock = new Mock<ILightChain>();
            mock.Setup(lc => lc.GetCurrentBlockHeightAsync()).Returns(Task.FromResult((ulong)_headers.Count - 1 + GlobalConfig.GenesisBlockHeight));
            mock.Setup(lc => lc.GetHeaderByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p =>
                {
                    return Task.FromResult(_sideChainHeaders[(int) p - 1]);
                });

            return mock;
        }

        private Mock<IBlockChain> MockBlockChain()
        {
            Mock<IBlockChain> mock = new Mock<IBlockChain>();
            mock.Setup(bc => bc.GetBlockByHeightAsync(It.IsAny<ulong>()))
                .Returns<ulong>(p => Task.FromResult(_blocks[(int) p - 1]));
            return mock;
        }

        private Mock<IChainService> MockChainService()
        {
            Mock<IChainService> mock = new Mock<IChainService>();
            mock.Setup(cs => cs.GetLightChain(It.IsAny<Hash>())).Returns(MockLightChain().Object);
            mock.Setup(cs => cs.GetBlockChain(It.IsAny<Hash>())).Returns(MockBlockChain().Object);
            return mock;
        }

        private IBlockHeader MockBlockHeader()
        {
            return new BlockHeader
            {
                MerkleTreeRootOfTransactions = Hash.Generate(),
                SideChainTransactionsRoot = Hash.Generate(),
                SideChainBlockHeadersRoot = Hash.Generate(),
                ChainId = Hash.Generate(),
                PreviousBlockHash = Hash.Generate(),
                MerkleTreeRootOfWorldState = Hash.Generate()
            };
        }

        private IBlockBody MockBlockBody(ulong height, Hash chainId = null)
        {
            return new BlockBody
            {
                IndexedInfo = { MockSideChainBlockInfo(height, chainId)}
            };
        }

        private SideChainBlockInfo MockSideChainBlockInfo(ulong height, Hash chainId = null)
        {
            return new SideChainBlockInfo
            {
                Height = height,
                ChainId = chainId ?? Hash.Generate(),
                TransactionMKRoot = Hash.Generate(),
                BlockHeaderHash = Hash.Generate()
            };
        }
        
        public Mock<IBlock> MockBlock(IBlockHeader header, IBlockBody body)
        {
            Mock<IBlock> mock = new Mock<IBlock>();
            mock.Setup(b => b.Header).Returns((BlockHeader)header);
            mock.Setup(b => b.Body).Returns((BlockBody)body);
            return mock;
        }

        private Mock<IBinaryMerkleTreeDao> MockBinaryMerkleTreeManager()
        {
            Mock<IBinaryMerkleTreeDao> mock = new Mock<IBinaryMerkleTreeDao>();
            mock.Setup(b => b.GetSideChainTransactionRootsMerkleTreeByHeightAsync(It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns<Hash, ulong>((_, u) =>
                {
                    _blocks[(int) u - 1].Body.CalculateMerkleTreeRoots();
                    return Task.FromResult(_blocks[(int) u - 1].Body.BinaryMerkleTreeForSideChainTransactionRoots);
                });
            return mock;
        }
        
        public ParentChainBlockInfoRpcServerImpl MockParentChainBlockInfoRpcServerImpl()
        {
            return new ParentChainBlockInfoRpcServerImpl(MockChainService().Object, _logger, MockBinaryMerkleTreeManager().Object);
        }

        public SideChainBlockInfoRpcServerImpl MockSideChainBlockInfoRpcServerImpl()
        {
            return new SideChainBlockInfoRpcServerImpl(MockChainService().Object, _logger);
        }
        
        public ServerManager ServerManager(ParentChainBlockInfoRpcServerImpl impl1, SideChainBlockInfoRpcServerImpl impl2)
        {
            return new ServerManager(impl1, impl2, _logger);
        }
        
        public Mock<IChainDao> MockChainManager()
        {
            var mock = new Mock<IChainDao>();
            mock.Setup(cm => cm.GetCurrentBlockHeightAsync(It.IsAny<Hash>())).Returns(() =>
            {
                var k = _i;
                return Task.FromResult(k);
            });
            mock.Setup(cm => cm.UpdateCurrentBlockHeightAsync(It.IsAny<Hash>(), It.IsAny<ulong>()))
                .Returns<Hash, ulong>((h, u) =>
                {
                    _i = u;
                    return Task.CompletedTask;
                });
            return mock;
        }

        public ClientManager MinerClientManager()
        {
            return new ClientManager(_logger, MockChainManager().Object, MockCrossChainInfo().Object);
        }

        public ulong GetTimes = 0;
        private Mock<ICrossChainInfo> MockCrossChainInfo()
        {
            var mock = new Mock<ICrossChainInfo>();
            mock.Setup(m => m.GetParentChainCurrentHeight()).Returns(() => GetTimes);
            return mock;
        }

        public void MockKeyPair(Hash chainId, string dir)
        {
            
            var certificateStore = new CertificateStore(dir);
            var name = chainId.DumpHex();
            var keyPair = certificateStore.WriteKeyAndCertificate(name, "127.0.0.1");
        }
        
        public Hash MockSideChainServer(int port, string address, string dir)
        {
            _sideChainHeaders = new List<IBlockHeader>
            {
                MockBlockHeader(),
                MockBlockHeader(),
                MockBlockHeader()
            };
            
            var sideChainId = Hash.Generate();
            ChainConfig.Instance.ChainId = sideChainId.DumpHex();
            
            MockKeyPair(sideChainId, dir);
            GrpcLocalConfig.Instance.LocalSideChainServerPort = port;
            GrpcLocalConfig.Instance.LocalServerIP = address;
            GrpcLocalConfig.Instance.SideChainServer = true;
            //start server, sidechain is server-side
            
            return sideChainId;
        }

        public Hash MockParentChainServer(int port, string address, string dir, Hash chainId = null)
        {
            
            chainId = chainId??Hash.Generate();
            
            _headers = new List<IBlockHeader>
            {
                MockBlockHeader(),
                MockBlockHeader(),
                MockBlockHeader()
            };
            //IBlockHeader blockHeader = Headers[0];
            _blocks = new List<IBlock>
            {
                MockBlock(_headers[0], MockBlockBody(GlobalConfig.GenesisBlockHeight, chainId)).Object,
                MockBlock(_headers[1], MockBlockBody(GlobalConfig.GenesisBlockHeight + 1, chainId)).Object,
                MockBlock(_headers[2], MockBlockBody(GlobalConfig.GenesisBlockHeight + 2, chainId)).Object
            };

            MockKeyPair(chainId, dir);
            GrpcLocalConfig.Instance.LocalParentChainServerPort = port;
            GrpcLocalConfig.Instance.LocalServerIP = address;
            GrpcLocalConfig.Instance.ParentChainServer = true;
            ChainConfig.Instance.ChainId = chainId.DumpHex();
            
            return chainId;
        }

        private Mock<IBlockValidationService> MockBlockValidationService()
        {
            var mock = new Mock<IBlockValidationService>();
            mock.Setup(bvs => bvs.ValidateBlockAsync(It.IsAny<IBlock>(), It.IsAny<IChainContext>()))
                .Returns(() => Task.FromResult(BlockValidationResult.Success));
            return mock;
        }
        
        public void ClearDirectory(string dir)
        {
            if(Directory.Exists(Path.Combine(dir, "certs")))
                Directory.Delete(Path.Combine(dir, "certs"), true);
        }
    }
}