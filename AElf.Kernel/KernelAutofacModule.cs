using AElf.Kernel.Persistence;
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
            
            builder.RegisterType<SmartContractStore>().As<ISmartContractStore>().SingleInstance();
            builder.RegisterType<TransactionStore>().As<ITransactionStore>().SingleInstance();
            builder.RegisterType<TransactionResultStore>().As<ITransactionResultStore>().SingleInstance();
            builder.RegisterType<BlockStore>().As<IBlockStore>().SingleInstance();
            builder.RegisterType<ChainStore>().As<IChainStore>().SingleInstance();
            builder.RegisterType<BinaryMerkleTreeStore>().As<IBinaryMerkleTreeStore>().SingleInstance();
            builder.RegisterType<LightChainCanonicalStore>().As<ILightChainCanonicalStore>().SingleInstance();
            builder.RegisterType<FunctionMetadataStore>().As<IFunctionMetadataStore>().SingleInstance();
        }
    }
}