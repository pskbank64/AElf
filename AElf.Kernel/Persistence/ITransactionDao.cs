using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Persistence
{
    public interface ITransactionDao
    {
        Task<Hash> AddTransactionAsync(Transaction tx);
        Task<Transaction> GetTransaction(Hash txId);
        Task RemoveTransaction(Hash txId);
        //Task<List<Transaction>> RollbackTransactions(Hash chainId, ulong currentHeight, ulong specificHeight);
    }
}