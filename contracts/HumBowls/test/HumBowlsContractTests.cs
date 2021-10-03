using System.Collections.Generic;
using System.Linq;

using FluentAssertions;
using Neo.Assertions;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Neo.BlockchainToolkit.SmartContract;
using Neo.SmartContract;
using Neo.VM;
using NeoTestHarness;
using Xunit;

namespace HumBowlsTests
{
    [CheckpointPath("test/bin/checkpoints/contract-deployed.neoxp-checkpoint")]
    public class HumBowlsContractTests : IClassFixture<CheckpointFixture<HumBowlsContractTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;

        public HumBowlsContractTests(CheckpointFixture<HumBowlsContractTests> fixture)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain("HumBowlsTests.neo-express");
        }
    }
}
