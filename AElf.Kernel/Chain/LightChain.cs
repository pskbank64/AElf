﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Persistence;
using Akka.Dispatch;
using Akka.Util;
using Easy.MessageHub;
using NLog;

namespace AElf.Kernel
{
    public class LightChain : ILightChain
    {
        protected readonly Hash _chainId;
        protected readonly IChainStore ChainStore;
        protected readonly IBlockStore BlockStore;
        protected readonly ILightChainCanonicalStore LightChainCanonicalStore;

        private readonly ILogger _logger;
        
        public LightChain(Hash chainId,
            IChainStore chainStore,
            IBlockStore blockStore, ILightChainCanonicalStore lightChainCanonicalStore, ILogger logger = null)
        {
            _chainId = chainId.Clone();
            ChainStore = chainStore;
            BlockStore = blockStore;
            LightChainCanonicalStore = lightChainCanonicalStore;
            _logger = logger;
        }

        public async Task<ulong> GetCurrentBlockHeightAsync()
        {
            var hash = await ChainStore.GetCurrentBlockHashAsync(_chainId);
            if (hash.IsNull())
            {
                return GlobalConfig.GenesisBlockHeight;
            }
            var header = (BlockHeader) await GetHeaderByHashAsync(hash);
            return header.Index;
        }
        
        public async Task<Hash> GetCurrentBlockHashAsync()
        {
            var hash = await ChainStore.GetCurrentBlockHashAsync(_chainId);
            return hash;
        }

        public async Task<bool> HasHeader(Hash blockHash)
        {
            var header = await BlockStore.GetBlockHeaderAsync(blockHash);
            return header != null;
        }

        public async Task AddHeadersAsync(IEnumerable<IBlockHeader> headers)
        {
            foreach (var header in headers)
            {
                await AddHeaderAsync(header);
            }
        }

        public async Task<IBlockHeader> GetHeaderByHashAsync(Hash blockHash)
        {
            return await BlockStore.GetBlockHeaderAsync(blockHash);
        }

        public async Task<IBlockHeader> GetHeaderByHeightAsync(ulong height)
        {
            var blockHash = await GetCanonicalHashAsync(height);
            if (blockHash == null)
            {
                return null;
            }

            return await GetHeaderByHashAsync(blockHash.OfType(HashType.BlockHeaderHash));
        }

        public async Task<bool> IsOnCanonical(Hash blockId)
        {
            var header = (BlockHeader) await GetHeaderByHashAsync(blockId);
            if (header == null)
            {
                return false;
            }

            var canonicalHash = await GetCanonicalHashAsync(header.Index);
            return canonicalHash == blockId;
        }

        protected async Task AddHeaderAsync(IBlockHeader header)
        {
            await CheckHeaderAppendable(header);
            await BlockStore.AddBlockHeaderAsync((BlockHeader) header);
            await MaybeSwitchBranch(header);
            MessageHub.Instance.Publish((BlockHeader) header);
        }

        protected Hash GetHeightHash(ulong height)
        {
            return DataPath.CalculatePointerForGettingBlockHashByHeight(_chainId, height);
        }

        public async Task<Hash> GetCanonicalHashAsync(ulong height)
        {
            var blockHash = await LightChainCanonicalStore.GetAsync(GetHeightHash(height).OfType(HashType.CanonicalHash));
            return blockHash;
        }

        protected async Task CheckHeaderAppendable(IBlockHeader header)
        {
            var blockHeader = (BlockHeader) header;

            #region genesis
            // TODO: more strict genesis
            if (blockHeader.Index == GlobalConfig.GenesisBlockHeight)
            {
                var curHash = await ChainStore.GetCurrentBlockHashAsync(_chainId);
                if (curHash.IsNull())
                {
                    await ChainStore.AddChainAsync(_chainId, header.GetHash());
                }
                return;
            }

            #endregion genesis
            var prevHeader = await GetHeaderByHashAsync(blockHeader.PreviousBlockHash);
            if (prevHeader == null)
            {
                throw new InvalidOperationException($"Parent is unknown for {blockHeader}.");
            }

            var expected = ((BlockHeader) prevHeader).Index + 1;
            var actual = blockHeader.Index; 
            
            if ( actual != expected )
            {
                throw new InvalidOperationException($"Incorrect index. Expected: {expected}, actual: {actual}");
            }
        }

        protected async Task<Tuple<List<IBlockHeader>, List<IBlockHeader>>> GetComparedBranchesAsync(
            IBlockHeader oldHead,
            IBlockHeader newHead)
        {
            var tempOldHead = (BlockHeader) oldHead;
            var tempNewHead = (BlockHeader) newHead;
            var oldBranch = new List<IBlockHeader>();
            var newBranch = new List<IBlockHeader>();
            while (((BlockHeader) oldHead).Index > ((BlockHeader) newHead).Index)
            {
                oldBranch.Add(tempOldHead);
                tempOldHead = (BlockHeader) await GetHeaderByHashAsync(tempOldHead.PreviousBlockHash);
            }

            while (((BlockHeader) newHead).Index > ((BlockHeader) oldHead).Index)
            {
                newBranch.Add(tempNewHead);
                if (tempNewHead == null)
                {
                    break;
                }
                tempNewHead = (BlockHeader) await GetHeaderByHashAsync(tempNewHead.PreviousBlockHash);
            }

            while (tempNewHead != null && tempOldHead.PreviousBlockHash != tempNewHead.PreviousBlockHash)
            {
                oldBranch.Add(tempOldHead);
                newBranch.Add(tempNewHead);
                tempOldHead = (BlockHeader) await GetHeaderByHashAsync(tempOldHead.PreviousBlockHash);
                tempNewHead = (BlockHeader) await GetHeaderByHashAsync(tempNewHead.PreviousBlockHash);
            }

            if (tempOldHead != null && tempNewHead != null)
            {
                oldBranch.Add(tempOldHead);
                newBranch.Add(tempNewHead);
            }

            return Tuple.Create(oldBranch, newBranch);
        }

        protected async Task MaybeSwitchBranch(IBlockHeader header)
        {
            var blockHeader = (BlockHeader) header;
            if (blockHeader.Index <= GlobalConfig.GenesisBlockHeight)
            {
                var hash = GetHeightHash(blockHeader.Index).OfType(HashType.CanonicalHash);
//                hash.Height = blockHeader.Index;
                await LightChainCanonicalStore.AddOrUpdateAsync(hash, header.GetHash());
                await ChainStore.UpdateCurrentBlockHashAsync(_chainId, header.GetHash());
                return;
            }
            
            var currentBlockHash = await GetCurrentBlockHashAsync();
            var currentHeader = await GetHeaderByHashAsync(currentBlockHash);
            if (currentHeader.GetHash().Equals(((BlockHeader) header).PreviousBlockHash) ||
                ((BlockHeader) header).PreviousBlockHash.Equals(Hash.Genesis))
            {
                var hash = GetHeightHash(((BlockHeader) header).Index).OfType(HashType.CanonicalHash);
//                hash.Height = ((BlockHeader) header).Index;
                await LightChainCanonicalStore.AddOrUpdateAsync(hash, header.GetHash());
                await ChainStore.UpdateCurrentBlockHashAsync(_chainId, header.GetHash());
                return;
            }

            if (((BlockHeader) header).Index > ((BlockHeader) currentHeader).Index)
            {
                await ChainStore.UpdateCurrentBlockHashAsync(_chainId, header.GetHash());
                var branches = await GetComparedBranchesAsync(currentHeader, header);
                if (branches.Item2.Count > 0)
                {
                    foreach (var newBranchHeader in branches.Item2)
                    {
                        if (newBranchHeader == null)
                        {
                            break;
                        }

                        var hash = GetHeightHash(((BlockHeader) newBranchHeader).Index).OfType(HashType.CanonicalHash);
//                        hash.Height = ((BlockHeader) newBranchHeader).Index;
                        await LightChainCanonicalStore.AddOrUpdateAsync(hash, newBranchHeader.GetHash());
                    }
                }
            }
        }


    }
}