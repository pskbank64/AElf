using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Persistence
{
    public interface IChainDao
    {
        Task AddChainAsync(Hash chainId, Hash genesisBlockHash);
        Task<Hash> GetGenesisBlockHashAsync(Hash chainId);
        Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash);
        Task<Hash> GetCurrentBlockHashAsync(Hash chainId);
        Task UpdateCurrentBlockHeightAsync(Hash chainId, ulong height);
        Task<ulong> GetCurrentBlockHeightAsync(Hash chainId);
        Task AddSideChainId(Hash chainId);
        Task<SideChainIdList> GetSideChainIdList();
    }
}