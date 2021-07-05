using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace OriginalHummusNFT
{
    [DisplayName("Original Hummus NFT")]
    [ManifestExtra("Author", "Humswap")]
    [ManifestExtra("Email", "info@humswap.org")]
    [ManifestExtra("Description", "Original Hummus is the first NFT from the Humswap Project")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    [ContractPermission("*", "transfer")]
    [ContractPermission("*", "winNFT")]
    public class OriginalHummusNFTContract : SmartContract
    {
        public class TokenState
        {
            public UInt160 Owner;
            public string Name;
            public string Description;
            public string Image;
            public string Series;
            public string Id;
        }

        static class Keys
        {
            public const string Owner = "o";
        }
        private static StorageMap Store => new StorageMap(Storage.CurrentContext, "DB_");

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        /// From, to, cost
        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;

        public static string id = "0001";
        public static string series = "Originals";

        [Safe]
        public static string Symbol() => "HUMMUS";

        [Safe]
        public static byte Decimals() => 0;

        [Safe]
        public static BigInteger TotalSupply() => 1;
        /*
        Base64 representation of the Original Hummus NFT
        */
        const string imageOfNFT = "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";

        private static void ValidateOwner()
        {
            ByteString owner = Store.Get(Keys.Owner);
            if (!Tx.Sender.Equals(owner))
            {
                throw new Exception("Only the contract owner can do this");
            }
        }

        public static BigInteger BalanceOf(UInt160 account)
        {
            if (Store.Get(Keys.Owner) == account)
            {
                return 1;
            }
            return 0;
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                Store.Put(Keys.Owner, (ByteString) Tx.Sender);
            }
        }

        public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            if (amount <= 0) throw new Exception("The parameter amount MUST be greater than 0.");
            // if (!Runtime.CheckWitness(from) && !from.Equals(Runtime.CallingScriptHash)) throw new Exception("No authorization.");
            ValidateOwner();
            OnTransfer(from, to, 1);
            Store.Put(Keys.Owner, (ByteString) to);
            if (ContractManagement.GetContract(to) != null)
                Contract.Call(to, "onNEP17Payment", CallFlags.All, new object[] { from, 1, data });
            return true;
        }

        // Winning an NFT means we have a new contract owner
        public static void WinNFT(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            if (amount <= 0) throw new Exception("The parameter amount MUST be greater than 0.");
            // if (!Runtime.CheckWitness(from) && !from.Equals(Runtime.CallingScriptHash)) throw new Exception("No authorization.");
            // ByteString owner = ContractMetadata.Get("Owner");
            // if (!Tx.Sender.Equals(owner))
            // {
            //     throw new Exception("Only the contract owner can do this");
            // }
            ValidateOwner();
            Store.Put(Keys.Owner, (ByteString) to);
            OnTransfer(from, to, 1);
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
