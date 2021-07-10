//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PurchaseTests {
    #if NETSTANDARD || NETFRAMEWORK || NETCOREAPP
    [System.CodeDom.Compiler.GeneratedCode("Neo.BuildTasks","1.0.37.24957")]
    #endif
    [System.ComponentModel.Description("PurchaseContract")]
    interface PurchaseContract {
        void onNEP17Payment(Neo.UInt160 @from, System.Numerics.BigInteger amount, object @data);
        void setPurchasePrice(System.Numerics.BigInteger price);
        void setCurrencyRequired(Neo.UInt160 currencyAddress);
        void setNFTToPurchase(Neo.UInt160 nftAddress);
        void genericTransferOut(System.Numerics.BigInteger amount, Neo.UInt160 to, Neo.UInt160 tokenAddress);
        void updateContract(byte[] nefFile, string manifest);
        void destroy();
        interface Events {
            void PurchasedNFT(Neo.UInt160 arg1, Neo.UInt160 arg2);
        }
    }
}