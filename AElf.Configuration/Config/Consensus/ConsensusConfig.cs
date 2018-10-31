using System;
using AElf.Common;
using AElf.Common.Enums;

// ReSharper disable InconsistentNaming
namespace AElf.Configuration.Config.Consensus
{
    public class ConsensusConfig : ConfigBase<ConsensusConfig>
    {
        public ConsensusType ConsensusType { get; set; }

        public int DPoSMiningInterval { get; set; }

        public int SingleNodeMiningInterval { get; set; }

        public int BlockProducerNumber { get; set; }

        public int BlockNumberOfEachRound => BlockProducerNumber + 1;

        public ConsensusConfig()
        {
            // By default.
            ConsensusType = ConsensusType.AElfDPoS;
            DPoSMiningInterval = 4000;
            SingleNodeMiningInterval = 4000;
            BlockProducerNumber = (8 + DateTime.UtcNow.Year - 2018) * 2 + 1;
        }
    }
}