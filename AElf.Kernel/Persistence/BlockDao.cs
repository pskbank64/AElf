using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;
using NLog;

namespace AElf.Kernel.Persistence
{
    public class BlockDao : IBlockDao
    {
        private readonly ILogger _logger;
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "Block";

        public BlockDao(IKeyValueDatabase database)
        {
            _database = database;
            _logger = LogManager.GetLogger(nameof(BlockDao));
        }

        public async Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody)
        {
            await _database.SetAsync(_dbName, blockHash.Clone().OfType(HashType.BlockBodyHash).DumpHex(), blockBody.ToByteArray());
        }

        public async Task<BlockBody> GetBlockBodyAsync(Hash bodyHash)
        {
            var data = await _database.GetAsync(_dbName, bodyHash.Clone().OfType(HashType.BlockBodyHash).DumpHex());
            return data?.Deserialize<BlockBody>();
        }

        public async Task<BlockHeader> AddBlockHeaderAsync(BlockHeader header)
        {
            await _database.SetAsync(_dbName, header.GetHash().OfType(HashType.BlockHeaderHash).DumpHex(), header.ToByteArray());
            return header;
        }
        
        public async Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash)
        {
            var data = await _database.GetAsync(_dbName,blockHash.Clone().OfType(HashType.BlockHeaderHash).DumpHex());
            return data?.Deserialize<BlockHeader>();
        }

        public async Task<Block> GetBlockAsync(Hash blockHash)
        {
            try
            {
                var header = await GetBlockHeaderAsync(blockHash);
                var bb = await GetBlockBodyAsync(blockHash);

                if (header == null || bb == null)
                    return null;

                return new Block { Header = header, Body = bb };
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"Error while getting block {blockHash.DumpHex()}.");
                return null;
            }
        }
    }
}