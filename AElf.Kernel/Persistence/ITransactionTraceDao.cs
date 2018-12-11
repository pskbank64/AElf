using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Persistence
{
    public interface ITransactionTraceDao
    {
        Task AddTransactionTraceAsync(TransactionTrace tr, Hash disambiguationHash = null);

        Task<TransactionTrace> GetTransactionTraceAsync(Hash txId, Hash disambiguationHash = null);
    }
}