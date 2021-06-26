using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace NFTSeller
{
    [DisplayName("YourName.NFTSellerContract")]
    [ManifestExtra("Author", "Your name")]
    [ManifestExtra("Email", "your@address.invalid")]
    [ManifestExtra("Description", "Describe your contract...")]
    public class NFTSellerContract : SmartContract
    {
        private static StorageMap ContractStorage => new StorageMap(Storage.CurrentContext, "NFTSellerContract");
        private static StorageMap ContractMetadata => new StorageMap(Storage.CurrentContext, "Metadata");
        private static StorageMap Offerings => new StorageMap(Storage.CurrentContext, "Offerings");
        private static StorageMap CurrentOffering => new StorageMap(Storage.CurrentContext, "CurrentOffering");

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        [DisplayName("NumberChanged")]
        public static event Action<UInt160, BigInteger> OnNumberChanged;

        public static bool ChangeNumber(BigInteger positiveNumber)
        {
            if (positiveNumber < 0)
            {
                throw new Exception("Only positive numbers are allowed.");
            }

            ContractStorage.Put(Tx.Sender, positiveNumber);
            OnNumberChanged(Tx.Sender, positiveNumber);
            return true;
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                ContractMetadata.Put("Owner", (ByteString) Tx.Sender);
            }
        }
        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            ByteString owner = ContractMetadata.Get("Owner");
            // The owner will only load the offering. 
            // In the future, let's plan to allow anyone to create their own offering.
            if (Tx.Sender.Equals(owner)) 
            {
                Offerings.Put(Runtime.CallingScriptHash, amount);
                CurrentOffering.Put("offering", Runtime.CallingScriptHash);
            } 
            else 
            {

            }
        }

        public static ByteString GetNumber()
        {
            return ContractStorage.Get(Tx.Sender);
        }

        public static void UpdateContract(ByteString nefFile, string manifest)
        {
            ByteString owner = ContractMetadata.Get("Owner");
            if (!Tx.Sender.Equals(owner))
            {
                throw new Exception("Only the contract owner can do this");
            }
            ContractManagement.Update(nefFile, manifest, null);
        }
    }
}
