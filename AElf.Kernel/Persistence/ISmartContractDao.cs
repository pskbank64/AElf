using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Persistence
{
    public interface ISmartContractDao
    {
        Task<SmartContractRegistration> GetAsync(Address contractAddress);
        Task InsertAsync(Address contractAddress, SmartContractRegistration reg);
    }
}