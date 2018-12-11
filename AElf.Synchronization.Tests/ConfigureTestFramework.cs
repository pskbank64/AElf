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
            builder.RegisterType<ChainDao>().As<IChainDao>().SingleInstance();
            builder.RegisterType<BlockDao>().As<IBlockDao>().SingleInstance();
            builder.RegisterType<TransactionDao>().As<ITransactionDao>().SingleInstance();
            builder.RegisterType<StateDao>().As<IStateDao>();
            builder.RegisterType<TxSignatureVerifier>().As<ITxSignatureVerifier>();
            builder.RegisterType<TxRefBlockValidator>().As<ITxRefBlockValidator>();
            builder.RegisterType<TxHub>().As<ITxHub>();
        }
    }
}