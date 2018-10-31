using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using NLog;
using AElf.Configuration.Config.Consensus;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable InconsistentNaming
    public class AElfDPoSObserver : IObserver<ConsensusBehavior>
    {
        private readonly ILogger _logger;

        private readonly Func<Task> _miningWithInitializingAElfDPoSInformation;
        private readonly Func<Task> _miningWithPublishingOutValueAndSignature;
        private readonly Func<Task> _publishInValue;
        private readonly Func<Task> _miningWithUpdatingAElfDPoSInformation;

        private static int Interval => ConsensusConfig.Instance.DPoSMiningInterval;
        private static int BPNumber => ConsensusConfig.Instance.BlockProducerNumber;
        /// <summary>
        /// Depends on how long this BP has to wait to be sure
        /// there's no other BP left to produce blocks.
        /// </summary>
        private static int MiningRecoverSlack => BPNumber * Interval;
        private static int TimeOfOneRound => (BPNumber + 2) * Interval;

        public AElfDPoSObserver(params Func<Task>[] miningFunctions)
        {
            if (miningFunctions.Length < 4)
            {
                throw new ArgumentException("Incorrect functions count.", nameof(miningFunctions));
            }

            _logger = LogManager.GetLogger(nameof(AElfDPoSObserver));

            _miningWithInitializingAElfDPoSInformation = miningFunctions[0];
            _miningWithPublishingOutValueAndSignature = miningFunctions[1];
            _publishInValue = miningFunctions[2];
            _miningWithUpdatingAElfDPoSInformation = miningFunctions[3];
        }

        public void OnCompleted()
        {
            _logger?.Trace($"{nameof(AElfDPoSObserver)} completed.");
        }

        public void OnError(Exception error)
        {
            _logger?.Error(error, $"{nameof(AElfDPoSObserver)} error.");
        }

        public void OnNext(ConsensusBehavior value)
        {
            switch (value)
            {
                case ConsensusBehavior.DoNothing:
                    _logger?.Trace("Start a new round though this behavior doing nothing.");
                    break;
                case ConsensusBehavior.InitializeAElfDPoS:
                    _miningWithInitializingAElfDPoSInformation();
                    break;
                case ConsensusBehavior.PublishOutValueAndSignature:
                    _miningWithPublishingOutValueAndSignature();
                    break;
                case ConsensusBehavior.PublishInValue:
                    _publishInValue();
                    break;
                case ConsensusBehavior.UpdateAElfDPoS:
                    _miningWithUpdatingAElfDPoSInformation();
                    break;
            }
        }

        public void Initialization()
        {
            Observable.Return(ConsensusBehavior.InitializeAElfDPoS).Subscribe(this);
        }

        public IDisposable RecoverMining()
        {
            var after = TimeSpan.FromMilliseconds(MiningRecoverSlack);
            var recoverMining = Observable
                .Timer(after)
                .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

            _logger?.Trace(
                $"Will try to produce extra block after {after} seconds to recover mining process, " +
                "this can be interupt by executing new block.");

            return Observable.Return(ConsensusBehavior.DoNothing)
                .Concat(recoverMining)
                .Subscribe(this);
        }

        public IDisposable SubscribeAElfDPoSMiningProcess(BlockProducer infoOfMe, Timestamp extraBlockTimeSlot)
        {
            _logger?.Trace("Extra block time slot of current round: " + extraBlockTimeSlot.ToDateTime().ToString("HH:mm:ss"));
            if (extraBlockTimeSlot.ToDateTime() < DateTime.UtcNow)
            {
                extraBlockTimeSlot = extraBlockTimeSlot.ToDateTime()
                    .AddMilliseconds(TimeOfOneRound)
                    .ToTimestamp();
                _logger?.Trace("Extra block time slot changed to: " +
                               extraBlockTimeSlot.ToDateTime().ToString("HH:mm:ss"));
            }

            var doNothingObservable = Observable
                .Timer(TimeSpan.FromSeconds(0))
                .Select(_ => ConsensusBehavior.DoNothing);

            var timeSlot = infoOfMe.TimeSlot;
            var now = DateTime.UtcNow.ToTimestamp();
            var distanceToProduceNormalBlock = (timeSlot - now).Seconds;

            IObservable<ConsensusBehavior> produceNormalBlock;
            if (distanceToProduceNormalBlock >= 0)
            {
                produceNormalBlock = Observable
                    .Timer(TimeSpan.FromSeconds(distanceToProduceNormalBlock))
                    .Select(_ => ConsensusBehavior.PublishOutValueAndSignature);

                if (distanceToProduceNormalBlock >= 0)
                {
                    _logger?.Trace($"Will produce normal block after {distanceToProduceNormalBlock} seconds");
                }
            }
            else
            {
                distanceToProduceNormalBlock = 0;
                produceNormalBlock = doNothingObservable;
            }

            var distanceToPublishInValue = (extraBlockTimeSlot - now).Seconds;

            IObservable<ConsensusBehavior> publishInValue;
            if (distanceToPublishInValue >= 0)
            {
                var after = distanceToPublishInValue - distanceToProduceNormalBlock;
                publishInValue = Observable
                    .Timer(TimeSpan.FromSeconds(after))
                    .Select(_ => ConsensusBehavior.PublishInValue);

                if (distanceToPublishInValue >= 0)
                {
                    _logger?.Trace($"Will publish in value after {distanceToPublishInValue} seconds");
                }
            }
            else
            {
                publishInValue = doNothingObservable;
            }

            IObservable<ConsensusBehavior> produceExtraBlock;
            if (distanceToPublishInValue < 0 && ConsensusConfig.Instance.BlockProducerNumber != 1)
            {
                produceExtraBlock = doNothingObservable;
                if (ConsensusConfig.Instance.BlockProducerNumber != 1)
                {
                    produceExtraBlock = doNothingObservable;
                }
            }
            else if (infoOfMe.IsEBP)
            {
                var after = distanceToPublishInValue + Interval / 1000;
                produceExtraBlock = Observable
                    .Timer(TimeSpan.FromMilliseconds(Interval))
                    .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

                if (after >= 0)
                {
                    _logger?.Trace($"Will produce extra block after {after} seconds");
                }
            }
            else
            {
                var after = distanceToPublishInValue + Interval / 1000 +
                            Interval * infoOfMe.Order / 1000 +
                            Interval / 750;
                produceExtraBlock = Observable
                    .Timer(TimeSpan.FromMilliseconds(Interval +
                                                     Interval * infoOfMe.Order +
                                                     Interval / 2))
                    .Select(_ => ConsensusBehavior.UpdateAElfDPoS);

                if (after >= 0)
                {
                    _logger?.Trace($"Will help to produce extra block after {after} seconds");
                }
            }

            return Observable.Return(ConsensusBehavior.DoNothing)
                .Concat(produceNormalBlock)
                .Concat(publishInValue)
                .Concat(produceExtraBlock)
                .SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(this);
        }
    }
}