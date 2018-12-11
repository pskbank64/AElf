using AElf.ChainController;
using AElf.Kernel.Persistence;
using AElf.SmartContract;

namespace AElf.Execution.Execution
{
    public class ServicePack
    {
        public IResourceUsageDetectionService ResourceDetectionService { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IStateDao StateDao { get; set; }
        public ITransactionTraceDao TransactionTraceDao { get; set; }
    }
}
