using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Persistence
{
    public interface ITransactionResultStore
    {
        Task AddTransactionResultAsync(TransactionResult tr);
        Task<TransactionResult> GetTransactionResultAsync(Hash txId);
    }
}