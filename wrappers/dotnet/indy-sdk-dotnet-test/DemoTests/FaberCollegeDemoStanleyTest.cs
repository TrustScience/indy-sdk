using Hyperledger.Indy.AnonCredsApi;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Hyperledger.Indy.IndySDKModels;

namespace Hyperledger.Indy.Test.DemoTests
{
    [TestClass]
    public class FaberCollegeDemoStanleyTest : IndyIntegrationTestBase
    {
        JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        readonly string poolName = $"pool-{Guid.NewGuid()}".ToLower();
        Pool poolHandle;

        [TestMethod]
        public async Task RunFaberCollegeDemo()
        {
            // Step 2: Connecting to the Indy Nodes Pool
            var pool = await CreatePool(poolName);

            // Step 3: Getting the ownership for Stewards's Verinym
            DidEntity steward = await RestoreSteward();

            // Step 4: Onboarding Faber, Acme, Thrift and Government by Steward

            // 4a. Connecting the Establishment

            // ## steward side
            // create pair wise identifier for steward to faber
            var stewardFaberDid = await Did.CreateAndStoreMyDidAsync(steward.Wallet, "{}");
            var nymRequest = await Ledger.BuildNymRequestAsync(steward.DidInfo.Did, stewardFaberDid.Did, stewardFaberDid.VerKey, null, null);
            await Ledger.SignAndSubmitRequestAsync(pool, steward.Wallet, steward.DidInfo.Did, nymRequest);

            // create connection request for faber college
            var connectionRequest = new ConnectionRequest()
            {
                Did = stewardFaberDid.Did,
                Nonce = 123456789
            };

            // pretend we sent the connectio request to faber college

            // ## faber side
            await Wallet.CreateWalletAsync(poolName, "faber_wallet", null, null, null);
            var faberWallet = await Wallet.OpenWalletAsync("faber_wallet", null, null);
            var faberStewardDidResult = await Did.CreateAndStoreMyDidAsync(faberWallet, "{}");
            var faberStewardDidInfo = new DidInfo()
            {
                Did = faberStewardDidResult.Did,
                VerKey = faberStewardDidResult.VerKey
            };

            var connectionResponse = new ConnectionResponse()
            {
                Did = faberStewardDidInfo.Did,
                VerKey = faberStewardDidInfo.VerKey,
                Nonce = connectionRequest.Nonce
            };

            // ask ledger for steward faber verKey
            var stewardFaberVerKey = await Did.KeyForDidAsync(poolHandle, faberWallet, connectionRequest.Did);
            // encrypt the response
            var anonEncryptedConnectionResponse = await Crypto.AnonCryptAsync(stewardFaberVerKey, connectionResponse.ToJsonThenBytes());

            // pretend we sent the connection response to steward

            // ## steward side
            // decrypt the response
            var decryptedConnectionResponse = (await Crypto.AnonDecryptAsync(steward.Wallet, stewardFaberDid.VerKey, anonEncryptedConnectionResponse)).Serialize<ConnectionResponse>();

            if (connectionRequest.Nonce != decryptedConnectionResponse.Nonce)
            {
                throw new Exception("Nonce not matched");
            }

            // create pair wise identifier for faber to steward
            var faberStewardDidNymRequest = await Ledger.BuildNymRequestAsync(steward.DidInfo.Did, decryptedConnectionResponse.Did, decryptedConnectionResponse.VerKey, null, null);
            await Ledger.SignAndSubmitRequestAsync(pool, steward.Wallet, steward.DidInfo.Did, faberStewardDidNymRequest);


            // 4b. faber getting Verinym. Steward creating faber's did on faber's behalf

            // faber side
            var faberDid = await Did.CreateAndStoreMyDidAsync(faberWallet, "{}");

            var faberDidInfo = new DidInfo()
            {
                Did = faberDid.Did,
                VerKey = faberDid.VerKey
            };

            var createDidRequestMessage = new Message()
            {
                SenderDidInfo = faberStewardDidInfo,
                Payload = faberDidInfo.ToJson()
            };

            // Faber authenticates and encrypts the message by calling crypto.auth_crypt using verkeys created for secure communication with Steward.
            // The Authenticated-encryption schema is designed for the sending of a confidential message specifically for a Recipient, using the 
            // Sender's public key. Using the Recipient's public key, the Sender can compute a shared secret key. Using the Sender's public key and
            // his secret key, the Recipient can compute the exact same shared secret key. That shared secret key can be used to verify that the encrypted 
            // message was not tampered with, before eventually decrypting it.
            var authCryptedFaberDidInfoJson = await Crypto.AuthCryptAsync(faberWallet, faberStewardDidInfo.VerKey, stewardFaberDid.VerKey, createDidRequestMessage.ToJsonThenBytes());

            // pretend faber sent the message to stewardc

            // steward side

            // decrypt the message with private key
            var decryptedResult = await Crypto.AuthDecryptAsync(steward.Wallet, stewardFaberVerKey, authCryptedFaberDidInfoJson);
            var decryptedMessage = decryptedResult.MessageData.Serialize<Message>();


            // authenticate by looking up faberStewardVerKey on ledger and compare with the one in message
            var faberStewardVerKey = await Did.KeyForDidAsync(pool, steward.Wallet, decryptedMessage.SenderDidInfo.Did);
            if (decryptedResult.TheirVk != faberStewardVerKey)
            {
                throw new Exception("Nonce not matched");
            }

            var decryptedFaberDidInfo = JsonConvert.DeserializeObject<DidInfo>(decryptedMessage.Payload);
            var faberDidNymRequest = await Ledger.BuildNymRequestAsync(steward.DidInfo.Did, decryptedConnectionResponse.Did, decryptedConnectionResponse.VerKey, null, null);
            await Ledger.SignAndSubmitRequestAsync(pool, steward.Wallet, steward.DidInfo.Did, faberDidNymRequest);

            // At this point Faber has a DID related to his identity in the Ledger.


        }



        private async Task<Pool> CreatePool(string poolName)
        {
            FileStream genesisTxnFile = PoolUtils.CreateGenesisTxnFile("genesis.txn");
            string PoolGenesisTxnPath = Path.GetFullPath(genesisTxnFile.Name).Replace('\\', '/');
            GenesisTransactionPath poolConfig = new GenesisTransactionPath { GenesisTransaction = PoolGenesisTxnPath };
            OpenPoolConfiguration openPoolConfiguration = new OpenPoolConfiguration { RefreshOnOpen = true };
            string poolConfigJSON = poolConfig.ToJson();
            string openPoolConfigurationJSON = openPoolConfiguration.ToJson();
            await Pool.CreatePoolLedgerConfigAsync(poolName, poolConfigJSON);
            poolHandle = await Pool.OpenPoolLedgerAsync(poolName, openPoolConfigurationJSON);
            return poolHandle;
        }

        private async Task<DidEntity> RestoreSteward()
        {
            string stewardWalletName = "sovrin_steward_wallet";
            await Wallet.CreateWalletAsync(poolName, stewardWalletName, null, null, null);
            Wallet stewardWallet = await Wallet.OpenWalletAsync(stewardWalletName, null, null);
            // we know the seed beforehand, creating wallet will give us the private key
            DidInfo stewardDidInfo = new DidInfo
            {
                Seed = "000000000000000000000000Steward1"
            };
            string stewardDidInfoJSON = stewardDidInfo.ToJson();
            CreateAndStoreMyDidResult stewardDidResult = await Did.CreateAndStoreMyDidAsync(stewardWallet, stewardDidInfoJSON);
            return new DidEntity
            {
                DidInfo = new DidInfo
                {
                    Did = stewardDidResult.Did,
                    VerKey = stewardDidResult.VerKey
                },
                Wallet = stewardWallet
            };
        }

    }


    public class Message
    {
        public DidInfo SenderDidInfo { get; set; }
        public string Payload { get; set; }
    }

    public static class ExtensionUtil
    {
        static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        public static string ToJson(this object model)
        {
            return JsonConvert.SerializeObject(model, Formatting.Indented, _jsonSerializerSettings);
        }
        public static T Serialize<T>(this byte[] bytes)
        {
            var str = bytes.ToStringValue();
            return JsonConvert.DeserializeObject<T>(str);
        }
        public static byte[] ToJsonThenBytes(this object model)
        {
            var json = model.ToJson();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }

        public static string ToStringValue(this byte[] bytes)
        {
            var str = Encoding.UTF8.GetString(bytes);
            return str;
        }
    }
}
