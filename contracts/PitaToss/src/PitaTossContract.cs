using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace PitaToss
{
    [DisplayName("Hummus.PitaTossContract")]
    [ManifestExtra("Author", "Hummus")]
    [ManifestExtra("Email", "your@address.invalid")]
    [ManifestExtra("Description", "Toss Pita and win NFTs. Tossing costs 0.1 GAS")]
    [ContractPermission("*", "onNEP17Payment")]
    [ContractPermission("*", "transfer")]
    [ContractPermission("*", "winNFT")]

    public class PitaTossContract : SmartContract
    {
        static class Keys
        {
            public const string DefaultNFT = "d";
            public const string Owner = "o";
            public const string PrizeNFT = "p";
            public const string Range = "r";
        }
        private static StorageMap Store => new StorageMap(Storage.CurrentContext, "x");

        // Mark: Events
        // random number, address that sent GAS
        public static event Action<uint, UInt160> SendRandomNumber;
        // NFT address, address of the winner
        public static event Action<UInt160, UInt160> WinNFT;
        public static event Action<UInt160> NewPrizeNFTLoaded;
        public static event Action<UInt160> NewDefaultNFTLoaded;

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;
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
            Sending payment to the contract initiates a "Pita Toss"
            For now we check a single number and if it is above this number we consider it a prize win.
            For a more granular game, we may increase the range, and or add addional conditionals.
            To increase user interaction, we could request a number as well.
        */
        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            // If someone is sending GAS, play Pita Toss
            if (Runtime.CallingScriptHash == GAS.Hash)
            {
                if (amount < 10000000) throw new Exception("Not enough GAS");
                var tx = (Transaction)Runtime.ScriptContainer;
                uint nonce = tx.Nonce >> 1;
                var stringInt = (string)Store.Get(Keys.Range);
                uint range = uint.Parse(stringInt);
                uint randomNumber = nonce % range;
                SendRandomNumber(randomNumber, tx.Sender);
                if (randomNumber > 20)
                {
                    // Win the PrizeNFT
                    var winningNFT = (UInt160)Store.Get(Keys.PrizeNFT);
                    WinNFT(winningNFT, tx.Sender);
                    if (winningNFT is not null && ContractManagement.GetContract(winningNFT) is not null)
                        Contract.Call(winningNFT, "winNFT", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, tx.Sender, 1, data });
                }
                else 
                {
                    // Win DefaultNFT
                    var winningNFT = (UInt160)Store.Get(Keys.DefaultNFT);
                    WinNFT(winningNFT, tx.Sender);
                    if (winningNFT is not null && ContractManagement.GetContract(winningNFT) is not null)
                        Contract.Call(winningNFT, "winNFT", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, tx.Sender, 1, data });
                }
            }
            // else 
            // {
            //     // Otherwise, we are loading the contract with an NFT
            //     if (IsOwner()) 
            //     {
            //         Store.Put(Keys.PrizeNFT, (ByteString) Runtime.CallingScriptHash);
            //         NewNFTLoaded(Runtime.CallingScriptHash);
            //     }
            //     else 
            //     {
            //         throw new Exception("Only the contract owner can do this");
            //     }
            // }
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                Store.Put(Keys.Owner, (ByteString) Tx.Sender);
                Store.Put(Keys.Range, (uint) 100);
            }
        }

        public static void SetPrizeNFT(UInt160 nftAddress)
        {
            ValidateOwner();
            Store.Put(Keys.PrizeNFT, (ByteString) nftAddress);
            NewPrizeNFTLoaded(nftAddress);
        }

        public static void SetDefaultNFT(UInt160 nftAddress)
        {
            ValidateOwner();
            Store.Put(Keys.DefaultNFT, (ByteString) nftAddress);
            NewDefaultNFTLoaded(nftAddress);
        }

        public static void TransferGASOut(BigInteger amount, UInt160 to) 
        {
            ValidateOwner();
            GAS.Transfer(Runtime.ExecutingScriptHash, to, amount);
        }

        public static void GenericTransferOut(BigInteger amount, UInt160 to, UInt160 tokenAddress) 
        {
            ValidateOwner();
            if (to is not null && ContractManagement.GetContract(tokenAddress) is not null)
                Contract.Call(tokenAddress, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, to, amount });
        }

        public static void UpdateRange(uint range) 
        {
            ValidateOwner();
            Store.Put("range", (uint) range);
        }

        public static void UpdateContract(ByteString nefFile, string manifest)
        {
            ValidateOwner();
            ContractManagement.Update(nefFile, manifest, null);
        }

        public static void Destroy()
        {
            ValidateOwner();
            ContractManagement.Destroy();
        }
    }
}
