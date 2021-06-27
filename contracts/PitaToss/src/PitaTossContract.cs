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
    [DisplayName("YourName.PitaTossContract")]
    [ManifestExtra("Author", "Your name")]
    [ManifestExtra("Email", "your@address.invalid")]
    [ManifestExtra("Description", "Describe your contract...")]
    [ContractPermission("*", "onNEP17Payment")]
    public class PitaTossContract : SmartContract
    {
        private static StorageMap ContractStorage => new StorageMap(Storage.CurrentContext, "PitaTossContract");
        private static StorageMap ContractMetadata => new StorageMap(Storage.CurrentContext, "Metadata");
        public static event Action<uint, UInt160> SendRandomNumber;
        public static event Action<UInt160> WinNFT;

        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        /*
            Sending payment to the contract initiates a "Pita Toss"
            For now we check a single number and if it is above this number we consider it a win.
            For a more granular game, we may increase the range, and or add addional conditionals.
            To increase user interaction, we could request a number as well.
        */
        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            var tx = (Transaction)Runtime.ScriptContainer;
            uint nonce = tx.Nonce >> 1;
            uint range = 100; //(uint)StdLib.Deserialize(ContractMetadata.Get("range"));
            uint randomNumber = nonce % range;
            SendRandomNumber(randomNumber, tx.Sender);
            if (randomNumber > 90) {
                // Win NFT
                WinNFT(tx.Sender);
            }
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (!update)
            {
                ContractMetadata.Put("Owner", (ByteString) Tx.Sender);
                ContractMetadata.Put("range", (uint) 100);
            }
        }

        public static void TransferOut(BigInteger amount, UInt160 thisAddress, UInt160 to) 
        {
            ByteString owner = ContractMetadata.Get("Owner");
            if (!Tx.Sender.Equals(owner))
            {
                throw new Exception("Only the contract owner can do this");
            }
            GAS.Transfer(Runtime.ExecutingScriptHash, thisAddress, amount);
        }

        public static void UpdateRange(uint range) 
        {
            ByteString owner = ContractMetadata.Get("Owner");
            if (!Tx.Sender.Equals(owner))
            {
                throw new Exception("Only the contract owner can do this");
            }
            ContractMetadata.Put("range", (uint) range);
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

        public static void Destroy()
        {
            ByteString owner = ContractMetadata.Get("Owner");
            if (!Tx.Sender.Equals(owner))
            {
                throw new Exception("Only the contract owner can do this");
            }
            ContractManagement.Destroy();
        }
    }
}
