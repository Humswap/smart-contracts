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

namespace JalapenoHummusNFTTests
{
    [CheckpointPath("test/bin/checkpoints/contract-deployed.neoxp-checkpoint")]
    public class JalapenoHummusNFTContractTests : IClassFixture<CheckpointFixture<JalapenoHummusNFTContractTests>>
    {
        readonly CheckpointFixture fixture;
        readonly ExpressChain chain;

        public JalapenoHummusNFTContractTests(CheckpointFixture<JalapenoHummusNFTContractTests> fixture)
        {
            this.fixture = fixture;
            this.chain = fixture.FindChain("JalapenoHummusNFTTests.neo-express");
        }

        [Fact]
        public void contract_owner_in_storage()
        {
            var settings = chain.GetProtocolSettings();
            var owner = chain.GetDefaultAccount("owner").ToScriptHash(settings.AddressVersion);

            using var snapshot = fixture.GetSnapshot();

            // check to make sure contract owner stored in contract storage
            var storages = snapshot.GetContractStorages<JalapenoHummusNFTContract>();
            storages.Count().Should().Be(1);
            storages.TryGetValue("MetadataOwner", out var item).Should().BeTrue();
            item!.Should().Be(owner);
        }
    }
}
