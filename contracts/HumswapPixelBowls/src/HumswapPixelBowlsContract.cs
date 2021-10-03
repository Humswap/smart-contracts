using System;
using System.ComponentModel;
using System.Numerics;

using Neo;
using Neo.SmartContract;
using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Native;
using Neo.SmartContract.Framework.Services;

namespace HumswapPixelBowls
{
    [DisplayName("Humswap Pixel Bowls")]
    [ManifestExtra("Author", "Humswap")]
    [ManifestExtra("Email", "info@humswap.org")]
    [ManifestExtra("Description", "Humswap Pixel Bowls")]
    [SupportedStandards("NEP-11")]
    [ContractPermission("*", "onNEP17Payment")]
    [ContractPermission("*", "onNEP11Payment")]
    [ContractPermission("*", "transfer")]
    [ContractPermission("*", "mint")]
    public class HumswapPixelBowls : SmartContract
    {

        #region Token Settings
        static readonly int MaxSupply = 2000000;
        static readonly int InitialSupply = 0;
        #endregion

        private class TokenState {
            public UInt160 Owner;
            public string Name;
            public string Image;
            public string TokenURI;
            public ByteString TokenId;
            public ByteString BuildId;
            
        }

        private static class TotalSupplyStorage
        {
            private static StorageMap Store => new StorageMap(Storage.CurrentContext, "DB_");
            public static readonly string key = "totalSupply";
            public static void Increase(BigInteger value) => Put(Get() + value);
            public static void Reduce(BigInteger value) => Put(Get() - value);
            public static void Put(BigInteger value) => Store.Put(key, value);
            public static BigInteger Get() => (BigInteger)Store.Get(key);

        }

        private static class AssetStorage
        {
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

        static class Keys
        {
            public const string Owner = "o";
            public const string MintingLive = "m";
            public const string Price = "p";
            public const string Minter = "minter";
        }

        private static bool ValidateAddress(UInt160 address) => address.IsValid && !address.IsZero;

        [Safe]
        public static string Symbol() => "PXBL";

        [Safe]
        public static byte Decimals() => 0;

        [Safe]
        public static BigInteger TotalSupply() => TotalSupplyStorage.Get();

        [Safe]
        public static BigInteger BalanceOf(UInt160 account)
        {
            if (!ValidateAddress(account)) throw new Exception("The parameters account SHOULD be a 20-byte non-zero address.");
            return AssetStorage.Get(account);
        }

        private static StorageMap Store => new StorageMap(Storage.CurrentContext, "DB_");
        private static Transaction Tx => (Transaction) Runtime.ScriptContainer;

        public delegate void OnTransferDelegate(UInt160 from, UInt160 to, BigInteger amount, ByteString tokenId);

        [DisplayName("Transfer")]
        public static event OnTransferDelegate OnTransfer;

        public static event Action<BigInteger> OnUpdateTotalSupply;
        public static event Action<string, string, string, UInt160, ByteString, ByteString> OnProperties;
        public static event Action<UInt160, ByteString, ByteString> OnMinted;

        protected const byte Prefix_Token = 0x03;
        protected const byte Prefix_AccountToken = 0x04;

        [Safe]
        public static UInt160 OwnerOf(ByteString tokenId)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            return token.Owner;
        }

        public static void EmitProperties(ByteString tokenId)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            if (tokenMap[tokenId] is null) throw new Exception("tokenId does not exist");

            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            OnProperties(token.Name, token.Image, token.TokenURI, token.Owner, token.TokenId, token.BuildId);
        }

        [Safe]
        public static Map<string, object> Properties(ByteString tokenId)
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            if (tokenMap[tokenId] is null) throw new Exception("tokenId does not exist");

            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            Map<string, object> map = new();
            map["name"] = token.Name;
            map["image"] = token.Image;
            map["tokenURI"] = token.TokenURI;
            map["owner"] = token.Owner;
            map["tokenId"] = token.TokenId;
            map["buildId"] = token.BuildId;
            return map;
        }

        [Safe]
        public static List<object> Snapshot() {
            List<object> byteStringArr = new List<object>();

            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountToken);
            var iterator = accountMap.Find(FindOptions.KeysOnly | FindOptions.RemovePrefix);

            while (iterator.Next())
            {
                byteStringArr.Add(iterator.Value);
            }
        
            return byteStringArr;
        }

        [Safe]
        public static Iterator Tokens()
        {
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            return tokenMap.Find(FindOptions.KeysOnly);
        }

        [Safe]
        public static Iterator TokensOf(UInt160 owner)
        {
            if (owner is null || !owner.IsValid)
                throw new Exception("The argument \"owner\" is invalid");
            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountToken);
            return accountMap.Find(owner, FindOptions.KeysOnly | FindOptions.RemovePrefix);
        }

        /*
            If you want to deal directly with an array, call this method. This is a convenience
            method to display all token ids in a List, as opposed to an Iterator.
        */
        [Safe]
        public static List<object> ListTokensOf(UInt160 owner)
        {
            List<object> byteStringArr = new List<object>();

            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountToken);
            var newAddress = owner;
            var iterator = accountMap.Find(owner, FindOptions.KeysOnly | FindOptions.RemovePrefix);

            while (iterator.Next())
            {
                byteStringArr.Add(iterator.Value);
            }
        
            return byteStringArr;
        }

        public static bool Transfer(UInt160 to, ByteString tokenId, object data)
        {
            if (to is null || !to.IsValid)
                throw new Exception("The argument \"to\" is invalid.");
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);
            TokenState token = (TokenState)StdLib.Deserialize(tokenMap[tokenId]);
            UInt160 from = token.Owner;
            if (!Runtime.CheckWitness(from)) return false;
            if (from != to)
            {
                token.Owner = to;
                tokenMap[tokenId] = StdLib.Serialize(token);
                AssetStorage.Reduce(from, 1);
                AssetStorage.Increase(to, 1);

                StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountToken);
                ByteString keyTo = to + tokenId;
                ByteString keyFrom = from + tokenId;

                accountMap[keyTo] = "v";
                accountMap.Delete(keyFrom);
            }

            PostTransfer(from, to, tokenId, data);
            return true;
        }

        protected static ByteString RandomId()
        {
            var tx = (Transaction)Runtime.ScriptContainer;
            BigInteger nonce = (BigInteger) tx.Nonce >> 1;
            ByteString value = (ByteString) nonce;
            return CryptoLib.Sha256(value);
        }

        public static void OnNEP17Payment(UInt160 from, BigInteger amount, object data)
        {
            if (!PublicMintingLive()) throw new Exception("Minting is not live.");
            if (Runtime.CallingScriptHash == GAS.Hash)
            {
                string bInt = ((BigInteger)Store.Get(Keys.Price)).ToString();
                uint price = uint.Parse(bInt);
                if (amount < price) throw new Exception("Not enough GAS");
                SubMint(from);
            }
        }
        public static void Mint(UInt160 forAddress)
        {
            if (DidReachMaxSupply()) throw new Exception("All NFTs have been minted.");
            if (!HasMintAccess()) throw new Exception("No access to Mint.");
            SubMint(forAddress);
        }

        private static void SubMint(UInt160 forAddress)
        {
            // First we update balance and total supply
            AssetStorage.Increase(forAddress, 1);
            
            BigInteger currentSupply = TotalSupplyStorage.Get();
            BigInteger updatedSupply = currentSupply + 1;
            OnUpdateTotalSupply(updatedSupply);
            TotalSupplyStorage.Put(updatedSupply);

            ByteString tokenId = (ByteString)updatedSupply;
            ByteString buildId = RandomId();
            TokenState token = new TokenState();

            // Create token state
            token.Owner = forAddress;
            token.Name = "Humswap Pixel Bowl";
            token.Image = $"https://www.humswap.org/image/pixelbowl/{updatedSupply}";
            token.TokenURI = $"https://www.humswap.org/data/pixelbowl/{updatedSupply}";
            token.TokenId = tokenId;
            token.BuildId = buildId;

            // Map to store token state
            StorageMap tokenMap = new(Storage.CurrentContext, Prefix_Token);

            // under this scenario we are minting
            StorageMap accountMap = new(Storage.CurrentContext, Prefix_AccountToken);
            ByteString keyTo = forAddress + tokenId;
            accountMap[keyTo] = "v";

            // Then we store the token. We use the order that it was minted as the key
            tokenMap[tokenId] = StdLib.Serialize(token);
            OnMinted(token.Owner, tokenId, buildId);
            PostTransfer(null, token.Owner, tokenId, null);
        }

        public static void OwnerMint()
        {
            if (!IsOwner()) throw new Exception("Only the owner can mint custom NFTs.");
            SubMint(Tx.Sender);
        }

        public static void Airdrop(UInt160 forAddress)
        {
            if (!IsOwner()) throw new Exception("Only the owner can mint custom NFTs.");
            SubMint(forAddress);
        }

        private static void UpdateTotalSupply() {
            BigInteger currentSupply = TotalSupplyStorage.Get();
            BigInteger updatedSupply = currentSupply + 1;
            OnUpdateTotalSupply(updatedSupply);
            TotalSupplyStorage.Increase(1);
        }

        private static bool DidReachMaxSupply() {
            BigInteger currentSupply = TotalSupplyStorage.Get();
            var currentMax = (BigInteger) MaxSupply;
            if (currentSupply < currentMax) {
                return false;
            }
            return true;
        }

        private static void PostTransfer(UInt160 from, UInt160 to, ByteString tokenId, object data)
        {
            OnTransfer(from, to, 1, tokenId);
            if (to is not null && ContractManagement.GetContract(to) is not null)
                Contract.Call(to, "onNEP11Payment", CallFlags.All, from, 1, tokenId, data);
        }

        private static bool IsOwner()
        {
            UInt160 owner = (UInt160) Store.Get(Keys.Owner);
            if (Runtime.CheckWitness(owner))
            {
                return true;
            }
            return false;
        }

        private static bool PublicMintingLive()
        {
            // We are using BigInteger: 1 = true ; 0 = false
            BigInteger mintingLive = (BigInteger) Store.Get(Keys.MintingLive);
            if (mintingLive == 1) 
            {
                return true;
            }
            return false;
        }

        private static bool HasMintAccess()
        {
            UInt160 minter = (UInt160) Store.Get(Keys.Minter);
            if (Runtime.CheckWitness(minter))
            {
                return true;
            }
            return false;
        }

        public static void SetMinter(UInt160 address)
        {
            if (IsOwner()) {
                Store.Put(Keys.Minter, (UInt160) address);
            } else {
                throw new Exception("Only the owner can set the minter.");
            }
        }

        public static void SetMintingLive(BigInteger mintingLive)
        {
            if (IsOwner()) {
                Store.Put(Keys.MintingLive, mintingLive);
            } else {
                throw new Exception("Only the owner can set the minter.");
            }
        }

        public static void TransferGASOut(BigInteger amount, UInt160 to) 
        {
            if (!IsOwner()) throw new Exception("Only the owner can perform this action.");
            GAS.Transfer(Runtime.ExecutingScriptHash, to, amount);
        }

        public static void GenericTransferOut(BigInteger amount, UInt160 to, UInt160 tokenAddress) 
        {
            if (!IsOwner()) throw new Exception("Only the owner can perform this action.");
            if (to is not null && ContractManagement.GetContract(tokenAddress) is not null)
                Contract.Call(tokenAddress, "transfer", CallFlags.All, new object[] { Runtime.ExecutingScriptHash, to, amount });
        }

        public static void UpdatePrice(BigInteger price) 
        {
            if (!IsOwner()) throw new Exception("Only the owner can perform this action.");
            Store.Put(Keys.Price, (BigInteger) price);
        }

        public static void Update(ByteString nefFile, string manifest)
        {
            if (!IsOwner()) throw new Exception("Only the owner can perform this action.");
            ContractManagement.Update(nefFile, manifest, null);
        }

        public static void Destroy()
        {
            if (!IsOwner()) throw new Exception("Only the owner can perform this action.");
            ContractManagement.Destroy();
        }

        [DisplayName("_deploy")]
        public static void Deploy(object data, bool update)
        {
            if (update) return;

            TotalSupplyStorage.Increase(InitialSupply);
            Store.Put(Keys.Owner, (UInt160) Tx.Sender);
            Store.Put(Keys.MintingLive, (BigInteger) 0);
            Store.Put(Keys.Price, (BigInteger) 200000000);
        }
    }
}