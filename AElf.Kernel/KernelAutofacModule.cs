using AElf.Kernel.Managers;
using AElf.Kernel.Persistence;
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
            
            builder.RegisterType<SmartContractDao>().As<ISmartContractDao>().SingleInstance();
            builder.RegisterType<TransactionDao>().As<ITransactionDao>().SingleInstance();
            builder.RegisterType<TransactionResultDao>().As<ITransactionResultDao>().SingleInstance();
            builder.RegisterType<BlockDao>().As<IBlockDao>().SingleInstance();
            builder.RegisterType<ChainDao>().As<IChainDao>().SingleInstance();
            builder.RegisterType<BinaryMerkleTreeDao>().As<IBinaryMerkleTreeDao>().SingleInstance();
            builder.RegisterType<DataStore>().As<IDataStore>().SingleInstance();
            builder.RegisterType<LightChainCanonicalDao>().As<ILightChainCanonicalDao>().SingleInstance();
            builder.RegisterType<FunctionMetadataDao>().As<IFunctionMetadataDao>().SingleInstance();
        }
    }
}