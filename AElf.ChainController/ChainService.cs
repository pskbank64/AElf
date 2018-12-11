using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Persistence;
using Akka.Dispatch;
using ServiceStack;

namespace AElf.ChainController
{
    public class ChainService : IChainService
    {
        private readonly IChainStore _chainManager;
        private readonly IBlockStore _blockManager;
        private readonly ITransactionStore _transactionStore;
        private readonly ITransactionTraceStore _transactionTraceStore;
        private readonly IStateStore _stateStore;
        private readonly ILightChainCanonicalStore _lightChainCanonicalStore;

        private readonly ConcurrentDictionary<Hash, BlockChain> _blockchains = new ConcurrentDictionary<Hash, BlockChain>();

        public ChainService(IChainStore chainManager, IBlockStore blockManager,
            ITransactionStore transactionStore, ITransactionTraceStore transactionTraceStore, 
            IStateStore stateStore,ILightChainCanonicalStore lightChainCanonicalStore)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _transactionStore = transactionStore;
            _transactionTraceStore = transactionTraceStore;
            _stateStore = stateStore;
            _lightChainCanonicalStore = lightChainCanonicalStore;
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

            blockChain = new BlockChain(chainId, _chainManager, _blockManager, _transactionStore,
                _transactionTraceStore, _stateStore,_lightChainCanonicalStore);
            _blockchains.TryAdd(chainId, blockChain);
            return blockChain;
        }

        public ILightChain GetLightChain(Hash chainId)
        {
            return new LightChain(chainId, _chainManager, _blockManager, _lightChainCanonicalStore);
        }
    }
}