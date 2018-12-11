﻿using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;
using AElf.ChainController;
using AElf.SmartContract;
using AElf.Kernel.Tests;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ServiceStack;
using Xunit;
using AElf.Runtime.CSharp;
using Xunit.Frameworks.Autofac;
using AElf.Common;
using AElf.Database;

namespace AElf.Runtime.CSharp.Tests
{
    public class MockSetup
    {
        private static int _incrementId = 0;
        public ulong NewIncrementId()
        {
            var n = Interlocked.Increment(ref _incrementId);
            return (ulong)n;
        }

        public Hash ChainId1 { get; } = Hash.Generate();
        public Hash ChainId2 { get; } = Hash.Generate();
        public ISmartContractService SmartContractService;

        public IStateDao StateDao;
        public DataProvider DataProvider1;
        public DataProvider DataProvider2;

        public Address ContractAddress1 { get; } = Address.FromRawBytes(Hash.Generate().ToByteArray());
        public Address ContractAddress2 { get; } = Address.FromRawBytes(Hash.Generate().ToByteArray());

        private ISmartContractDao _smartContractDao;
        private IChainCreationService _chainCreationService;
        private IFunctionMetadataService _functionMetadataService;

        private ISmartContractRunnerFactory _smartContractRunnerFactory;

        public MockSetup(IStateDao stateDao, IChainCreationService chainCreationService, IFunctionMetadataService functionMetadataService, ISmartContractRunnerFactory smartContractRunnerFactory, IKeyValueDatabase database)
        {
            StateDao = stateDao;
            _chainCreationService = chainCreationService;
            _functionMetadataService = functionMetadataService;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _smartContractDao = new SmartContractDao(database);
            Task.Factory.StartNew(async () =>
            {
                await Init();
            }).Unwrap().Wait();
            SmartContractService = new SmartContractService(_smartContractDao, _smartContractRunnerFactory, stateDao, _functionMetadataService);
            Task.Factory.StartNew(async () =>
            {
                await DeploySampleContracts();
            }).Unwrap().Wait();
        }
        
        public byte[] SmartContractZeroCode
        {
            get
            {
                return ContractCodes.TestContractZeroCode;
            }
        }

        private async Task Init()
        {
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero,
                Type = (int)SmartContractType.BasicContractZero
            };

            var chain1 = await _chainCreationService.CreateNewChainAsync(ChainId1, new List<SmartContractRegistration>{reg});
            DataProvider1 = DataProvider.GetRootDataProvider(
                chain1.Id,
                Address.FromRawBytes(ChainId1.OfType(HashType.AccountZero).ToByteArray())
            );
            DataProvider1.StateDao = StateDao;

            var chain2 = await _chainCreationService.CreateNewChainAsync(ChainId2, new List<SmartContractRegistration>{reg});
            DataProvider2 = DataProvider.GetRootDataProvider(
                chain2.Id,
                Address.FromRawBytes(ChainId1.OfType(HashType.AccountZero).ToByteArray())
            );
            DataProvider2.StateDao = StateDao;
        }

        private async Task DeploySampleContracts()
        {
            var reg = new SmartContractRegistration
            {
                Category = 1,
                ContractBytes = ByteString.CopyFrom(ContractCode),
                ContractHash = Hash.FromRawBytes(ContractCode)
            };

            await SmartContractService.DeployContractAsync(ChainId1, ContractAddress1, reg, false);
            await SmartContractService.DeployContractAsync(ChainId2, ContractAddress2, reg, false);
        }

        public string SdkDir
        {
            get => "../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0";
        }
        
        public byte[] ContractCode
        {
            get
            {
                byte[] code = null;
                using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath($"{SdkDir}/AElf.Runtime.CSharp.Tests.TestContract.dll")))
                {
                    code = file.ReadFully();
                }
                return code;
            }
        }
    }
}
