using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Persistence
{
    public class BinaryMerkleTreeStore : IBinaryMerkleTreeStore
    {
        private readonly IKeyValueDatabase _database;
        private const string _dbName = "BinaryMerkleTree";

        public BinaryMerkleTreeStore(IKeyValueDatabase database)
        {
            _database = database;
        }

        /// <summary>
        /// Store <see cref="BinaryMerkleTree"/> for transactions.
        /// </summary>
        /// <param name="binaryMerkleTree"></param>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task AddTransactionsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForTransactionsMerkleTreeByHeight(chainId, height);
            await _database.SetAsync(_dbName, key.DumpHex(), binaryMerkleTree.ToByteArray());
        }

        /// <summary>
        /// Store <see cref="BinaryMerkleTree"/> for side chain transaction roots.
        /// </summary>
        /// <param name="binaryMerkleTree"></param>
        /// <param name="chainId">Parent chain Id</param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task AddSideChainTransactionRootsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, 
            Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForSideChainTxRootsMerkleTreeByHeight(chainId, height);
            await _database.SetAsync(_dbName, key.DumpHex(), binaryMerkleTree.ToByteArray());
        }

        /// <summary> 
        /// Get <see cref="BinaryMerkleTree"/> of transactions.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height">=</param>
        /// <returns></returns>
        public async Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForTransactionsMerkleTreeByHeight(chainId, height);
            var data = await _database.GetAsync(_dbName, key.DumpHex());

            return data?.Deserialize<BinaryMerkleTree>();
        }

        /// <summary> 
        /// Get <see cref="BinaryMerkleTree"/> of side chain transaction roots.
        /// </summary>
        /// <param name="chainId">Parent chain Id</param>
        /// <param name="height">Parent chain height</param>
        /// <returns></returns>
        public async Task<BinaryMerkleTree> GetSideChainTransactionRootsMerkleTreeByHeightAsync(Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForSideChainTxRootsMerkleTreeByHeight(chainId, height);
            var data = await _database.GetAsync(_dbName, key.DumpHex());

            return data?.Deserialize<BinaryMerkleTree>();
        }

        /// <summary>
        /// Add <see cref="MerklePath"/> for tx root of a block indexed by parent chain.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="chainId">Child chain id</param>
        /// <param name="height">Child chain height</param>
        /// <returns></returns>
        public async Task AddIndexedTxRootMerklePathInParentChain(MerklePath path, Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForIndexedTxRootMerklePathByHeight(chainId, height);
            await _database.SetAsync(_dbName, key.DumpHex(), path.ToByteArray());
        }
        
        /// <summary>
        /// Add <see cref="MerklePath"/> for tx root of a block indexed by parent chain.
        /// </summary>
        /// <param name="chainId">Child chain id</param>
        /// <param name="height">Child chain height</param>
        /// <returns></returns>
        public async Task<MerklePath> GetIndexedTxRootMerklePathInParentChain(Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForIndexedTxRootMerklePathByHeight(chainId, height);
            var data = await _database.GetAsync(_dbName, key.DumpHex());

            return data?.Deserialize<MerklePath>();
        }
    }
}