using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace PitaToken
{
    [DisplayName("Pita Token")]
    [ManifestExtra("Author", "Humswap")]
    [ManifestExtra("Email", "info@humswap.org")]
    [ManifestExtra("Description", "Dip PITA in Hummus")]
    [SupportedStandards("NEP-17")]

    public class PitaTokenContract : SmartContract
    {

        #region Token Settings
        static readonly ulong MaxSupply = 1_000_000;
        static readonly ulong InitialSupply = 100_000;
        #endregion

        #region Notifications
        [DisplayName("Transfer")]
        public static event Action<UInt160, UInt160, BigInteger> OnTransfer;
        #endregion

        static class Keys
        {
            public const string Owner = "o";
        }

        public static class AssetStorage
        {
            public static readonly string mapName = "asset";

            public static void Increase(UInt160 key, BigInteger value) => Put(key, Get(key) + value);

            public static void Enable() => Store.Put("enable".ToByteArray(), 1);

            public static void Disable() => Store.Put("enable".ToByteArray(), 0);

            public static void Reduce(UInt160 key, BigInteger value)
            {
                var oldValue = Get(key);
                if (oldValue == value)
                    Remove(key);
                else
                    Put(key, oldValue - value);
            }

            public static void Put(UInt160 key, BigInteger value) => Store.Put((byte[])key, value);

            public static BigInteger Get(UInt160 key) => (BigInteger)Store.Get((byte[])key);

            public static bool GetPaymentStatus()
            {
                var enableValue = (BigInteger)Store.Get("enable");
                return enableValue.Equals(1);
            }

            public static void Remove(UInt160 key) => Store.Delete((byte[])key);
        }

        public static class TotalSupplyStorage
        {
            private static StorageMap Store => new StorageMap(Storage.CurrentContext, "DB_");

            public static readonly string mapName = "contract";

            public static readonly string key = "totalSupply";

            public static void Increase(BigInteger value) => Put(Get() + value);

            public static void Reduce(BigInteger value) => Put(Get() - value);

            public static void Put(BigInteger value) => Store.Put(key, value);

            public static BigInteger Get() => (BigInteger)Store.Get(key);

        }
        private static StorageMap Store => new StorageMap(Storage.CurrentContext, "DB_");
        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        private static bool ValidateAddress(UInt160 address) => address.IsValid && !address.IsZero;
        private static bool IsDeployed(UInt160 address) => ContractManagement.GetContract(address) != null;

        private static void ValidateOwner()
        {
            ByteString owner = Store.Get(Keys.Owner);
            if (!Tx.Sender.Equals(owner))
            {
                throw new Exception("Only the contract owner can do this");
            }
        }

        public static string Symbol() => "PITA";

        public static ulong Decimals() => 0;

        public static BigInteger TotalSupply() => TotalSupplyStorage.Get();

        public static BigInteger BalanceOf(UInt160 account)
        {
            if (!ValidateAddress(account)) throw new Exception("The parameters account SHOULD be a 20-byte non-zero address.");
            return AssetStorage.Get(account);
        }

        public static bool Transfer(UInt160 from, UInt160 to, BigInteger amount, object data)
        {
            if (!ValidateAddress(from) || !ValidateAddress(to)) throw new Exception("The parameters from and to SHOULD be 20-byte non-zero addresses.");
            if (amount <= 0) throw new Exception("The parameter amount MUST be greater than 0.");
            if (!Runtime.CheckWitness(from) && !from.Equals(Runtime.CallingScriptHash)) throw new Exception("No authorization.");
            if (AssetStorage.Get(from) < amount) throw new Exception("Insufficient balance.");
            if (from == to) return true;

            AssetStorage.Reduce(from, amount);
            AssetStorage.Increase(to, amount);

            OnTransfer(from, to, amount);

            // Validate payable
            if (IsDeployed(to)) Contract.Call(to, "onNEP17Payment", CallFlags.All, new object[] { from, amount, data });
            return true;
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            ValidateOwner();
            ContractManagement.Update(nefFile, manifest, null);
        }

        public static void Destroy()
        {
            ValidateOwner();
            ContractManagement.Destroy();
        }

        public static void EnablePayment()
        {
            ValidateOwner();
            AssetStorage.Enable();
        }

        public static void DisablePayment()
        {
            ValidateOwner();
            AssetStorage.Disable();
        }

        private static bool IsOwner()
        {
            ByteString owner = Store.Get(Keys.Owner);
            if (Tx.Sender.Equals(owner))
            {
                return true;
            }
            return false;
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;
            if (TotalSupplyStorage.Get() > 0) throw new Exception("Contract has been deployed.");

            TotalSupplyStorage.Increase(InitialSupply);
            AssetStorage.Increase(Tx.Sender, InitialSupply);
            Store.Put(Keys.Owner, (ByteString) Tx.Sender);

            OnTransfer(null, Tx.Sender, InitialSupply);
        }
    }
}
