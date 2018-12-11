using AElf.ChainController;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Persistence;
using AElf.Miner.TxMemPool;
using AElf.Synchronization.BlockSynchronization;
using Autofac;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Synchronization.Tests.ConfigureTestFramework", "AElf.Synchronization.Tests")]

namespace AElf.Synchronization.Tests
{
    public class ConfigureTestFramework : AutofacTestFramework
    {
        public ConfigureTestFramework(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new LoggerAutofacModule());
            builder.RegisterModule(new DatabaseAutofacModule());
            builder.RegisterType<BlockValidationService>().As<IBlockValidationService>().SingleInstance();
            builder.RegisterType<ChainContextService>().As<IChainContextService>().SingleInstance();
            builder.RegisterType<ChainService>().As<IChainService>().SingleInstance();
            builder.RegisterType<BlockSet>().As<IBlockSet>().SingleInstance();
            builder.RegisterType<ChainStore>().As<IChainStore>().SingleInstance();
            builder.RegisterType<BlockStore>().As<IBlockStore>().SingleInstance();
            builder.RegisterType<TransactionStore>().As<ITransactionStore>().SingleInstance();
            builder.RegisterType<StateStore>().As<IStateStore>();
            builder.RegisterType<TxSignatureVerifier>().As<ITxSignatureVerifier>();
            builder.RegisterType<TxRefBlockValidator>().As<ITxRefBlockValidator>();
            builder.RegisterType<TxHub>().As<ITxHub>();
        }
    }
}