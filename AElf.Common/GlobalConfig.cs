namespace AElf.Common
{
    // ReSharper disable InconsistentNaming
    public static class GlobalConfig
    {
        public static int AddressLength = 18;
        public const ulong GenesisBlockHeight = 1;
        public static readonly string GenesisSmartContractZeroAssemblyName = "AElf.Contracts.Genesis";
        public static readonly string GenesisConsensusContractAssemblyName = "AElf.Contracts.Consensus";
        public static readonly string GenesisTokenContractAssemblyName = "AElf.Contracts.Token";
        public static readonly string GenesisSideChainContractAssemblyName = "AElf.Contracts.SideChain";

        public static readonly ulong ReferenceBlockValidPeriod = 64;

        public static readonly string GenesisBasicContract = "BasicContractZero";

        public const int AElfLogInterval = 900;

        public const bool DoWeNeedToAppointDPoSGenerator = false;

        public static int MiningTimeout = 4000 * 9 / 10;
        
        #region AElf DPoS
        
        public static int MiningSlack = 8000;
        public const int AElfDPoSLogRoundCount = 1;
        public const string AElfDPoSCurrentRoundNumber = "__AElfCurrentRoundNumber__";
        public const string AElfDPoSBlockProducerString = "__AElfBlockProducer__";
        public const string AElfDPoSInformationString = "__AElfDPoSInformation__";
        public const string AElfDPoSExtraBlockProducerString = "__AElfExtraBlockProducer__";
        public const string AElfDPoSExtraBlockTimeSlotString = "__AElfExtraBlockTimeSlot__";
        public const string AElfDPoSFirstPlaceOfEachRoundString = "__AElfFirstPlaceOfEachRound__";
        public const string AElfDPoSMiningIntervalString = "__AElfDPoSMiningInterval__";
        public const string AElfDPoSMiningRoundHashMapString = "__AElfDPoSMiningRoundHashMap__";

        #endregion

        #region AElf Cross Chain
        public const string AElfTxRootMerklePathInParentChain = "__TxRootMerklePathInParentChain__";
        public const string AElfParentChainBlockInfo = "__ParentChainBlockInfo__";
        public const string AElfBoundParentChainHeight = "__BoundParentChainHeight__";
        public static readonly int AElfInitCrossChainRequestInterval = 4;
        
        #endregion

        #region PoTC

        public static ulong ExpectedTransactionCount = 8000;

        #endregion

        #region Single node test

        public static int SingleNodeTestMiningInterval = 4000;

        #endregion

        public static ulong BasicContractZeroSerialNumber = 0;

        #region data key prefixes

        public const string StatePrefix = "st";
        public const string TransactionReceiptPrefix = "rc";

        #endregion data key prefixes
    }
}