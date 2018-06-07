using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAgent
{
    public static class Config
    {
        public const string WebAppSeed = "000000000000000000000000Steward1";
        public const string WebAppPoolName = "webapppool999";
        public const string WebAppWalletName = "webappwallet1";
        public static string WebAppDid = "";
        public static string WebAppVerKey = "";
        public static Wallet WebAppWallet;
        public static Pool WebAppPool;
    }
}
