using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Persistence
{
    public class ChainDao : IChainDao
    {
        private readonly IKeyValueDatabase _database;
        private readonly ILogger _logger;
        private const string _dbName = "Chain";
        private readonly Hash _sideChainIdListKey = Hash.FromString("SideChainIdList");

        public ChainDao(IKeyValueDatabase database)
        {
            _database = database;
            _logger = LogManager.GetLogger(nameof(ChainDao));
        }

        public async Task AddChainAsync(Hash chainId, Hash genesisBlockHash)
        {
            await _database.SetAsync(_dbName, chainId.OfType(HashType.GenesisHash).DumpHex(), genesisBlockHash.ToByteArray());
            await UpdateCurrentBlockHashAsync(chainId, genesisBlockHash);
        }

        public async Task<Hash> GetGenesisBlockHashAsync(Hash chainId)
        {
            var data = await _database.GetAsync(_dbName,chainId.OfType(HashType.GenesisHash).DumpHex());
            return data?.Deserialize<Hash>();
        }

        public async Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash)
        {
            var key = chainId.OfType(HashType.CurrentHash);
            await _database.SetAsync(_dbName, key.DumpHex(), blockHash.ToByteArray());
        }
        
        public async Task<Hash> GetCurrentBlockHashAsync(Hash chainId)
        {
            var key = chainId.OfType(HashType.CurrentHash);
            var data = await _database.GetAsync(_dbName, key.DumpHex());
            return data?.Deserialize<Hash>();
        }
        
        /// <summary>
        /// update block count in this chain not last block index
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task UpdateCurrentBlockHeightAsync(Hash chainId, ulong height)
        {
            var key = chainId.OfType(HashType.ChainHeight);
            await _database.SetAsync(_dbName, key.DumpHex(), new UInt64Value
            {
                Value = height
            }.ToByteArray());
        }

        /// <summary>
        /// return block count in this chain not last block index
        /// "0" means no block recorded for this chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public async Task<ulong> GetCurrentBlockHeightAsync(Hash chainId)
        {
            var key = chainId.OfType(HashType.ChainHeight);
            var data = await _database.GetAsync(_dbName, key.DumpHex());
            var height = data?.Deserialize<UInt64Value>();
            return height?.Value ?? 0;
        }
        
        public async Task AddSideChainId(Hash chainId)
        {
            var idList = await GetSideChainIdList();
            idList = idList ?? new SideChainIdList();
            idList.ChainIds.Add(chainId);
            await _database.SetAsync(_dbName, _sideChainIdListKey.DumpHex(), idList.ToByteArray());
        }

        public async Task<SideChainIdList> GetSideChainIdList()
        {
            var data = await _database.GetAsync(_dbName, _sideChainIdListKey.DumpHex());
            return data?.Deserialize<SideChainIdList>();
        }
    }
}