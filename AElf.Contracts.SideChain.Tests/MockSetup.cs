    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using AElf.ChainController;
    using AElf.ChainController.CrossChain;
    using AElf.Execution;
    using AElf.Kernel;
    using AElf.SmartContract;
    using Google.Protobuf;
    using ServiceStack;
using    AElf.Common;
    using AElf.Database;
    using AElf.Execution.Execution;
    using AElf.Kernel.Persistence;
    using AElf.Miner.TxMemPool;
    using AElf.Runtime.CSharp;
    using AElf.SmartContract.Metadata;
    using NLog;

namespace AElf.Contracts.SideChain.Tests
    {
        public class MockSetup
        {
            // IncrementId is used to differentiate txn
            // which is identified by From/To/IncrementId
            private static int _incrementId = 0;
            public ulong NewIncrementId()
            {
                var n = Interlocked.Increment(ref _incrementId);
                return (ulong)n;
            }
    
            public Hash ChainId1 { get; } = Hash.FromString("ChainId1");
            public IStateStore StateStore { get; private set; }
            public ISmartContractStore SmartContractStore;
            public ISmartContractService SmartContractService;
            public IChainService ChainService;
            private IFunctionMetadataService _functionMetadataService;
    
            private IChainCreationService _chainCreationService;
    
            private ISmartContractRunnerFactory _smartContractRunnerFactory;
            private ILogger _logger;
            private IKeyValueDatabase _database;
            private ILightChainCanonicalStore _lightChainCanonicalStore;

            public MockSetup(ILogger logger)
            {
                _logger = logger;
                Initialize();
            }
    
            private void Initialize()
            {
                NewStorage();
                var transactionManager = new TransactionStore(_database);
                var transactionTraceManager = new TransactionTraceStore(_database);
                var callingGraphStore = new CallingGraphStore(_database);
                var functionMetadataStore = new FunctionMetadataStore(_database);
                _functionMetadataService = new FunctionMetadataService(callingGraphStore, functionMetadataStore, _logger);
                var chainManagerBasic = new ChainStore(_database);
                _lightChainCanonicalStore =new LightChainCanonicalStore(_database);
                ChainService = new ChainService(chainManagerBasic, new BlockStore(_database),
                    transactionManager, transactionTraceManager, StateStore, _lightChainCanonicalStore);
                _smartContractRunnerFactory = new SmartContractRunnerFactory();
                var runner = new SmartContractRunner("../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/");
                _smartContractRunnerFactory.AddRunner(0, runner);
                _chainCreationService = new ChainCreationService(ChainService,
                    new SmartContractService(new SmartContractStore(_database), _smartContractRunnerFactory,
                        StateStore, _functionMetadataService), _logger);
                SmartContractStore = new SmartContractStore(_database);
                Task.Factory.StartNew(async () =>
                {
                    await Init();
                }).Unwrap().Wait();
                SmartContractService = new SmartContractService(SmartContractStore, _smartContractRunnerFactory, StateStore, _functionMetadataService);
                ChainService = new ChainService(new ChainStore(_database), new BlockStore(_database), new TransactionStore(_database), new TransactionTraceStore(_database), StateStore,_lightChainCanonicalStore);
            }

            private void NewStorage()
            {
                _database = new InMemoryDatabase();
                StateStore = new StateStore(_database);
            }
            
            public byte[] SideChainCode
            {
                get
                {
                    byte[] code = null;
                    using (FileStream file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.SideChain/bin/Debug/netstandard2.0/AElf.Contracts.SideChain.dll")))
                    {
                        code = file.ReadFully();
                    }
                    return code;
                }
            }
            
            public byte[] SCZeroContractCode
            {
                get
                {
                    byte[] code = null;
                    using (FileStream file = File.OpenRead(Path.GetFullPath("../../../../AElf.Contracts.Genesis/bin/Debug/netstandard2.0/AElf.Contracts.Genesis.dll")))
                    {
                        code = file.ReadFully();
                    }
                    return code;
                }
            }
            
            private async Task Init()
            {
                var reg1 = new SmartContractRegistration
                {
                    Category = 0,
                    ContractBytes = ByteString.CopyFrom(SideChainCode),
                    ContractHash = Hash.FromRawBytes(SideChainCode),
                    Type = (int)SmartContractType.SideChainContract
                };
                var reg0 = new SmartContractRegistration
                {
                    Category = 0,
                    ContractBytes = ByteString.CopyFrom(SCZeroContractCode),
                    ContractHash = Hash.FromRawBytes(SCZeroContractCode),
                    Type = (int)SmartContractType.BasicContractZero
                };
    
                var chain1 =
                    await _chainCreationService.CreateNewChainAsync(ChainId1,
                        new List<SmartContractRegistration> {reg0, reg1});
            }
            
            public async Task<IExecutive> GetExecutiveAsync(Address address)
            {
                var executive = await SmartContractService.GetExecutiveAsync(address, ChainId1);
                return executive;
            }
        }
    }
