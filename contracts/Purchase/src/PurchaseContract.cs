using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace Purchase
{
    [DisplayName("Hummus.PurchaseContract")]
    [ManifestExtra("Author", "Hummus")]
    [ManifestExtra("Email", "your@address.invalid")]
    [ManifestExtra("Description", "Purchase NFTs")]
    [ContractPermission("*", "onNEP17Payment")]
    [ContractPermission("*", "transfer")]
    public class PurchaseContract : SmartContract
    {
        static class Keys
        {
            public const string Owner = "o";
            public const string NFTToPurchase = "n";
            public const string CurrencyRequired = "c";
            public const string PurchasePrice = "p";
        }

        // NFT Address, Purchaser
        public static event Action<UInt160, UInt160> PurchasedNFT;

        private static StorageMap Store => new StorageMap(Storage.CurrentContext, "x");

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

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            // Owner loads the NFT/Purchasable token
            if (IsOwner())
            {
                Store.Put(Keys.NFTToPurchase, Runtime.CallingScriptHash);
            }
            else
            {
                var currencyAddress = (UInt160)Store.Get(Keys.CurrencyRequired);
                var cost = (BigInteger)Store.Get(Keys.PurchasePrice);
                // Check for correct currency and purchase price
                if ((Runtime.CallingScriptHash == currencyAddress) && (amount >= cost)) {
                    PurchasedNFT(currencyAddress, from);
                    if (currencyAddress is not null && ContractManagement.GetContract(currencyAddress) is not null)
                        Contract.Call(currencyAddress, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, from, 1, data });
                }
                else
                {
                    throw new Exception("Incorrect currency or amount");
                }
            }
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                Store.Put(Keys.Owner, (ByteString) Tx.Sender);
            }
        }

        public static void SetPurchasePrice(BigInteger price)
        {
            ValidateOwner();
            Store.Put(Keys.PurchasePrice, (BigInteger) price);
        }

        public static void SetCurrencyRequired(UInt160 currencyAddress)
        {
            ValidateOwner();
            Store.Put(Keys.CurrencyRequired, (UInt160) currencyAddress);
        }

        public static void SetNFTToPurchase(UInt160 nftAddress)
        {
            ValidateOwner();
            Store.Put(Keys.NFTToPurchase, (UInt160) nftAddress);
        }

        public static void GenericTransferOut(BigInteger amount, UInt160 to, UInt160 tokenAddress) 
        {
            ValidateOwner();
            if (to is not null && ContractManagement.GetContract(tokenAddress) is not null)
                Contract.Call(tokenAddress, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, to, amount });
        }

        public static void UpdateContract(ByteString nefFile, string manifest)
        {
            ValidateOwner();
            ContractManagement.Update(nefFile, manifest, null);
        }
    }
}
