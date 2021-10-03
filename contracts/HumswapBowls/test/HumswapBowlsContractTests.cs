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

namespace HumswapBowlsTests
{
    [CheckpointPath("test/bin/checkpoints/contract-deployed.neoxp-checkpoint")]
    public class HumswapBowlsContractTests : IClassFixture<CheckpointFixture<HumswapBowlsContractTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;

        public HumswapBowlsContractTests(CheckpointFixture<HumswapBowlsContractTests> fixture)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain("HumswapBowlsTests.neo-express");
        }
    }
}
