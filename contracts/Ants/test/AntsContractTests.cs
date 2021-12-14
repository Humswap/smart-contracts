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

namespace AntsTests
{
    [CheckpointPath("test/bin/checkpoints/contract-deployed.neoxp-checkpoint")]
    public class AntsContractTests : IClassFixture<CheckpointFixture<AntsContractTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;

        public AntsContractTests(CheckpointFixture<AntsContractTests> fixture)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain("AntsTests.neo-express");
        }
    }
}
