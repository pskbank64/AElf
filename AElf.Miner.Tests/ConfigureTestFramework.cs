﻿using AElf.ChainController;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Database;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel;
using AElf.Kernel.Persistence;
using AElf.Miner.TxMemPool;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using AElf.Synchronization.BlockSynchronization;
using Autofac;
using Autofac.Core;
using Xunit;
using Xunit.Abstractions;
using Xunit.Frameworks.Autofac;

[assembly: TestFramework("AElf.Miner.Tests.ConfigureTestFramework", "AElf.Miner.Tests")]

namespace AElf.Miner.Tests
{
    public class ConfigureTestFramework : AutofacTestFramework
    {
        public ConfigureTestFramework(IMessageSink diagnosticMessageSink)
            : base(diagnosticMessageSink)
        {
        }

        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            ChainConfig.Instance.ChainId = Hash.Generate().DumpHex();
            NodeConfig.Instance.NodeAccount = Address.Generate().DumpHex();

            builder.RegisterModule(new LoggerAutofacModule());
            builder.RegisterModule(new DatabaseAutofacModule());
            builder.RegisterType<BlockValidationService>().As<IBlockValidationService>().SingleInstance();
            builder.RegisterType<ChainContextService>().As<IChainContextService>().SingleInstance();
            builder.RegisterType<ChainService>().As<IChainService>().SingleInstance();
            builder.RegisterType<BlockSet>().As<IBlockSet>().SingleInstance();
            builder.RegisterType<ChainStore>().As<IChainStore>().SingleInstance();
            builder.RegisterType<BlockStore>().As<IBlockStore>().SingleInstance();
            builder.RegisterType<TransactionStore>().As<ITransactionStore>().SingleInstance();
            builder.RegisterType<TransactionTraceStore>().As<ITransactionTraceStore>().SingleInstance();
            builder.RegisterType<StateStore>().As<IStateStore>();
            builder.RegisterType<TxSignatureVerifier>().As<ITxSignatureVerifier>();
            builder.RegisterType<TxRefBlockValidator>().As<ITxRefBlockValidator>();
        }
    }
}