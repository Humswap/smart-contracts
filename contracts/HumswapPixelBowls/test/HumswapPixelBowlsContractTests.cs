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

namespace HumswapPixelBowlsTests
{
    [CheckpointPath("test/bin/checkpoints/contract-deployed.neoxp-checkpoint")]
    public class HumswapPixelBowlsContractTests : IClassFixture<CheckpointFixture<HumswapPixelBowlsContractTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;

        public HumswapPixelBowlsContractTests(CheckpointFixture<HumswapPixelBowlsContractTests> fixture)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain("HumswapPixelBowlsTests.neo-express");
        }
    }
}
