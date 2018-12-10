using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using Autofac;

namespace AElf.Kernel
{
    public class KernelAutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var assembly1 = typeof(ISerializer<>).Assembly;
            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();
            
            var assembly2 = typeof(BlockHeader).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));
            
            builder.RegisterType<SmartContractDao>().As<ISmartContractDao>();
            builder.RegisterType<TransactionDao>().As<ITransactionDao>();
            builder.RegisterType<TransactionResultDao>().As<ITransactionResultDao>();
            builder.RegisterType<BlockDao>().As<IBlockDao>();
            builder.RegisterType<ChainDao>().As<IChainDao>();
            builder.RegisterType<BinaryMerkleTreeDao>().As<IBinaryMerkleTreeDao>();
            builder.RegisterType<DataStore>().As<IDataStore>();
        }
    }
}