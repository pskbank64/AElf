using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Persistence
{
    public interface IBinaryMerkleTreeDao
    {
        Task AddTransactionsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height);
        Task AddSideChainTransactionRootsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height);
        Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(Hash chainId, ulong height);
        Task<BinaryMerkleTree> GetSideChainTransactionRootsMerkleTreeByHeightAsync(Hash chainId, ulong height);
        Task AddIndexedTxRootMerklePathInParentChain(MerklePath path, Hash chainId, ulong height);
        Task<MerklePath> GetIndexedTxRootMerklePathInParentChain(Hash chainId, ulong height);
    }
}