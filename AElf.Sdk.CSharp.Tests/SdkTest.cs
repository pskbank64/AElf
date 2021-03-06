﻿using System;
using System.IO;
using System.Linq;
using AElf.Kernel;
using Xunit.Frameworks.Autofac;
using Xunit;
using AElf.Common;

namespace AElf.Sdk.CSharp.Tests
{
    [UseAutofacTestFramework]
    public class SdkTest
    {
        private TestContractShim _contractShim;
        public SdkTest(TestContractShim contractShim)
        {
            _contractShim = contractShim;
        }

        [Fact]
        public void Test()
        {
            uint ts = _contractShim.GetTotalSupply();
            Assert.Equal((uint)100, ts);
            string name = "AElf";
            Hash address = Hash.Generate();
            bool res = _contractShim.SetAccount(name, address);
            Assert.True(res);
            string resName = _contractShim.GetAccountName();
            Assert.Equal("AElf", resName);
        }

        [Fact]
        public void InlineCallTest()
        {
            var trace = _contractShim.InlineCallToZero();
            var expected =new Transaction()
            {
                From = Address.Zero,
                To = Address.Zero,
                MethodName = "Dummy"
            };
            Assert.Equal(expected, trace.InlineTransactions[0]);
        }
        // TODO: Add more test cases
    }
}
