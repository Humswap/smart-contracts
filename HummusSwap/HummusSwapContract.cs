using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace HummusSwap
{
    [DisplayName("GTeam.HummusSwapContract")]
    [ManifestExtra("Author", "Jason")]
    [ManifestExtra("Email", "hummusswap@gmail.com")]
    [ManifestExtra("Description", "For all your swapping needs")]
    [SupportedStandards("NEP-17")]
    [ContractPermission("*", "onNEP17Payment")]
    public class HummusSwapContract : SmartContract
    {

        /*
        @dex You can use block nonce.
 var tx = (Transaction)Runtime.ScriptContainer;
            uint nonce = tx.Nonce >> 1;
            uint range = 39;
            uint randomNumber = nonce % range;
            */
        private static StorageMap ContractStorage => new StorageMap(Storage.CurrentContext, "HummusSwapContract");
        private static StorageMap ContractMetadata => new StorageMap(Storage.CurrentContext, "Metadata");

        private static StorageMap CoinStorage => new StorageMap(Storage.CurrentContext, "CoinStorage");
        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        [InitialValue("NhS7aowjANr2T5SMXgDGzSePvsXq43zE7y", ContractParameterType.Hash160)]
        static readonly UInt160 Owner = default;
        private static bool IsOwner() => Runtime.CheckWitness(Owner);
        public static event Action<ByteString> AmountOfTokens;
        public static event Action<StorageMap> AllTokens;
        public static event Action<UInt160> OnOwnerChange;
        public static event Action<uint> SendRandomNumber;


        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
           CoinStorage.Put(Runtime.CallingScriptHash, amount);

            // if (ExecutionEngine. == NEO.Hash)
            // {
            //     CoinStorage.Put(ExecutionEngine.CallingScriptHash, amount);
            // }
            // else if (ExecutionEngine.CallingScriptHash == GAS.Hash)
            // {
            //     if (from != null) Mint(amount * TokensPerGAS);
            // }
            // else
            // {
            //     throw new Exception("Wrong calling script hash");
            // }
            
            
        }
        public static void _deploy(object data, bool update)
        {
            if (update) return;
            // var value = new UInt160();

            // if (Owner == value) {
            //     Owner = Runtime.CallingScriptHash;
            //     OnOwnerChange(Runtime.CallingScriptHash);
            // }


            // TotalSupplyStorage.Increase(InitialSupply);
            // AssetStorage.Increase(Owner, InitialSupply);

            // OnTransfer(null, Owner, InitialSupply);
        }

        public static uint randomNumber() {
            var tx = (Transaction)Runtime.ScriptContainer;
            uint nonce = tx.Nonce >> 1;
            uint range = 39;
            uint randomNumber = nonce % range;
            SendRandomNumber(randomNumber);
            return randomNumber;
        }

        public static void getOwner() {
            OnOwnerChange(Owner);
        }

        public static void TokenAmount(ByteString address) {
            AmountOfTokens(CoinStorage.Get(address));
            AllTokens(CoinStorage);
        }
        public static void Update(ByteString nefFile, string manifest, object data)
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Update(nefFile, manifest, data);
        }

        public static void Destroy()
        {
            if (!IsOwner()) throw new Exception("No authorization.");
            ContractManagement.Destroy();
        }
    }
}
