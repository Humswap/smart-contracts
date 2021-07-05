using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace Auction
{
    [DisplayName("Hummus.AuctionContract")]
    [ManifestExtra("Author", "Hummus")]
    [ManifestExtra("Email", "your@address.invalid")]
    [ManifestExtra("Description", "Hummus Auction Contract for NFTs")]
    [ContractPermission("*", "onNEP17Payment")]
    [ContractPermission("*", "transfer")]
    [ContractPermission("*", "winNFT")]
    public class AuctionContract : SmartContract
    {
        static class Keys
        {
            public const string Owner = "o";
            public const string NFTToWin = "n";
            public const string highestBid = "hb";
            public const string highestBidder = "hbr";
        }
        private static StorageMap Store => new StorageMap(Storage.CurrentContext, "x");

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        // Bid amount, highest bidder
        public static event Action<BigInteger, UInt160> NewHighestBid;
        // didWin, NFT address, address of the winner
        public static event Action<bool, UInt160, UInt160> WinNFT;
        public static event Action<UInt160> NewNFTLoaded;
        public static event Action<UInt160> LiveAuction;
        public static event Action<BigInteger, UInt160> HighestBidInfo;

        private static bool IsOwner()
        {
            ByteString owner = Store.Get(Keys.Owner);
            if (!Tx.Sender.Equals(owner))
            {
                return false;
            }
            return true;
        }

        private static void ValidateOwner()
        {
            ByteString owner = Store.Get(Keys.Owner);
            if (!Tx.Sender.Equals(owner))
            {
                throw new Exception("Only the contract owner can do this");
            }
        }

        /*
            At first we will only support GAS for bidding on NFTs
        */
        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            // Sending GAS attempts to place the highest bid
            if (Runtime.CallingScriptHash == GAS.Hash)
            {
                var highestBid = (BigInteger)Store.Get(Keys.highestBid);
                // If a user sends the highest GAS amount, they are the current highest bidder
                if (amount > highestBid) {

                    var previousHighestBidder = (UInt160)Store.Get(Keys.highestBidder);

                    // Refund the previous highestBid
                    GAS.Transfer(Runtime.ExecutingScriptHash, previousHighestBidder, highestBid);

                    // Now let's store everything
                    Store.Put(Keys.highestBid, (BigInteger) amount);
                    Store.Put(Keys.highestBidder, (UInt160) from);
                    NewHighestBid(amount, from);
                }
                else 
                {
                    throw new Exception("Your bid is not higher than the highest bid");
                }
            }
            else
            {
                // We will configure what is Auctionable
                if (IsOwner()) 
                {
                    Store.Put(Keys.NFTToWin, (ByteString) Runtime.CallingScriptHash);
                    NewNFTLoaded(Runtime.CallingScriptHash);
                }
                else 
                {
                    throw new Exception("Only the contract owner can do this");
                }
            }
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                Store.Put(Keys.Owner, (ByteString) Tx.Sender);
                Store.Put(Keys.highestBid, (BigInteger) 0);
            }
        }

        public static void UpdateContract(ByteString nefFile, string manifest)
        {
            ValidateOwner();
            ContractManagement.Update(nefFile, manifest, null);
        }

        // For now, ending an Auction is a manual process
        public static void EndAuction(object data) 
        {
            ValidateOwner();
            var highestBidder = (UInt160)Store.Get(Keys.highestBidder);
            var winningNFT = (UInt160)Store.Get(Keys.NFTToWin);
            WinNFT(true, winningNFT, highestBidder);
            if (winningNFT is not null && ContractManagement.GetContract(winningNFT) is not null)
                Contract.Call(winningNFT, "winNFT", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, highestBidder, 1, data });
            // Set to default state
            Store.Delete(Keys.highestBidder);
            Store.Delete(Keys.NFTToWin);
            Store.Put(Keys.highestBid, (BigInteger) 0);
        }

        public static void CurrentAuction()
        {
            var NFTToAuction = (UInt160)Store.Get(Keys.NFTToWin);
            LiveAuction(NFTToAuction);
        }

        public static void CurrentHighestBid()
        {
            var highestBid = (BigInteger)Store.Get(Keys.highestBid);
            var highestBidder =  (UInt160)Store.Get(Keys.highestBidder);
            HighestBidInfo(highestBid, highestBidder);
        }
    }
}
