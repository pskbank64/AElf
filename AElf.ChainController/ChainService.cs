using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Persistence;
using AElf.Kernel.Storages;
using Akka.Dispatch;
using ServiceStack;

namespace AElf.ChainController
{
    public class ChainService : IChainService
    {
        private readonly IChainDao _chainManager;
        private readonly IBlockDao _blockManager;
        private readonly ITransactionDao _transactionDao;
        private readonly ITransactionTraceDao _transactionTraceDao;
        private readonly IDataStore _dataStore;
        private readonly IStateDao _stateDao;
        private readonly ILightChainCanonicalDao _lightChainCanonicalDao;

        private readonly ConcurrentDictionary<Hash, BlockChain> _blockchains = new ConcurrentDictionary<Hash, BlockChain>();

        public ChainService(IChainDao chainManager, IBlockDao blockManager,
            ITransactionDao transactionDao, ITransactionTraceDao transactionTraceDao, 
            IDataStore dataStore, IStateDao stateDao,ILightChainCanonicalDao lightChainCanonicalDao)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _transactionDao = transactionDao;
            _transactionTraceDao = transactionTraceDao;
            _dataStore = dataStore;
            _stateDao = stateDao;
            _lightChainCanonicalDao = lightChainCanonicalDao;
        }

        public IBlockChain GetBlockChain(Hash chainId)
        {
            // To prevent some weird situations.
            if (chainId == Hash.Default && _blockchains.Any())
            {
                return _blockchains.First().Value;
            }
            
            if (_blockchains.TryGetValue(chainId, out var blockChain))
            {
                return blockChain;
            }

            blockChain = new BlockChain(chainId, _chainManager, _blockManager, _transactionDao,
                _transactionTraceDao, _stateDao, _dataStore,_lightChainCanonicalDao);
            _blockchains.TryAdd(chainId, blockChain);
            return blockChain;
        }

        public ILightChain GetLightChain(Hash chainId)
        {
            return new LightChain(chainId, _chainManager, _blockManager, _dataStore, _lightChainCanonicalDao);
        }
    }
}