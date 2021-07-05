//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OriginalHummusNFTTests {
    #if NETSTANDARD || NETFRAMEWORK || NETCOREAPP
    [System.CodeDom.Compiler.GeneratedCode("Neo.BuildTasks","1.0.37.24957")]
    #endif
    [System.ComponentModel.Description("OriginalHummusNFTContract")]
    interface OriginalHummusNFTContract {
        string symbol();
        System.Numerics.BigInteger decimals();
        System.Numerics.BigInteger totalSupply();
        System.Numerics.BigInteger balanceOf(Neo.UInt160 account);
        bool transfer(Neo.UInt160 @from, Neo.UInt160 to, System.Numerics.BigInteger amount, object @data);
        void winNFT(Neo.UInt160 @from, Neo.UInt160 to, System.Numerics.BigInteger amount, object @data);
        void updateContract(byte[] nefFile, string manifest);
        void destroy();
        interface Events {
            void Transfer(Neo.UInt160 arg1, Neo.UInt160 arg2, System.Numerics.BigInteger arg3);
        }
    }
}
