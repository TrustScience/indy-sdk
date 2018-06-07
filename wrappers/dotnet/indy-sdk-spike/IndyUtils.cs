using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hyperledger.Indy.IndySDKModels;

namespace indy_sdk_spike
{
    public class IndyUtils
    {
        public static async Task<(Wallet,string,string)> CreateWalletIfNotExist(string seed, string poolName, string walletName)
        {
            //var allWallets = (await Wallet.ListWalletsAsync()).Serialize<List<WalletMetadata>>();
            //var exists = allWallets.Any(x => x.Name.Equals(walletName, StringComparison.CurrentCultureIgnoreCase));

            try
            {
                await Wallet.CreateWalletAsync(poolName, walletName, null, null, null);
            }
            catch (Hyperledger.Indy.WalletApi.WalletExistsException) { } // ignore
            
            var wallet = await Wallet.OpenWalletAsync(walletName, null, null);
            DidInfo didInfo = new DidInfo
            {
                Seed = seed,
            };
            string didInfoJson = didInfo.ToJson();
            var didResult = await Did.CreateAndStoreMyDidAsync(wallet, didInfoJson);
            return (wallet, didResult.Did, didResult.VerKey);
        }

        public static async Task<Pool> CreatePool(string poolName)
        {
            FileStream genesisTxnFile = PoolUtils.CreateGenesisTxnFile("genesis.txn");
            string PoolGenesisTxnPath = Path.GetFullPath(genesisTxnFile.Name).Replace('\\', '/');
            GenesisTransactionPath poolConfig = new GenesisTransactionPath { GenesisTransaction = PoolGenesisTxnPath };
            OpenPoolConfiguration openPoolConfiguration = new OpenPoolConfiguration { RefreshOnOpen = true };
            string poolConfigJSON = poolConfig.ToJson();
            string openPoolConfigurationJSON = openPoolConfiguration.ToJson();

            try
            {
                await Pool.CreatePoolLedgerConfigAsync(poolName, poolConfigJSON);
            }
            catch (Hyperledger.Indy.PoolApi.PoolLedgerConfigExistsException) { } //ignore
            
            var pool = await Pool.OpenPoolLedgerAsync(poolName, openPoolConfigurationJSON);
            return pool;
        }
    }
}
