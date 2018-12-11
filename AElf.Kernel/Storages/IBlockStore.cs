using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Persistence
{
    public interface IBlockStore
    {
        Task<BlockHeader> AddBlockHeaderAsync(BlockHeader header);
        Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody);
        Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash);
        Task<BlockBody> GetBlockBodyAsync(Hash bodyHash);
        Task<Block> GetBlockAsync(Hash blockHash);
    }
}