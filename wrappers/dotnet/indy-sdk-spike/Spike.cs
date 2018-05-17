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
    public class SpikeApp
    {
        JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        readonly string poolName = $"pool-{Guid.NewGuid()}".ToLower();
        Pool pool;

        public async Task RuntrusteeCollegeDemo()
        {
            // Step 2: Connecting to the Indy Nodes Pool

            Console.WriteLine("Step 2: Connecting to the Indy Nodes Pool");
            pool = await CreatePool(poolName);

            // Step 3: Getting the ownership for Stewards's Verinym
            Console.WriteLine("Step 3: Getting the ownership for Stewards's Verinym");
            DidEntity stewardDidEntity = await RestoreSteward();

            // Step 4: Onboarding trustee, Acme, Thrift and Government by Steward
            Console.WriteLine("Step 4: Onboarding trustee, Acme, Thrift and Government by Steward");
            var faberInfo = await OnboardTrustee($"FaberColledge{Guid.NewGuid()}", stewardDidEntity);
            var acmeInfo = await OnboardTrustee($"AcmeCorp{Guid.NewGuid()}", stewardDidEntity);
            var thriftInfo = await OnboardTrustee($"ThriftBank{Guid.NewGuid()}", stewardDidEntity);
            var govInfo = await OnboardTrustee($"Government{Guid.NewGuid()}", stewardDidEntity);

            // Step 5: Credential Schema Setup
            Console.WriteLine("Step 5: Credential Schema Setup");
            // trustee (government) create and publish credential schema

            var transcriptSchemaId = await CreateSchema(govInfo.DidEntity.Wallet, govInfo.DidEntity.DidInfo.Did,
                "Transcript",
                "1.2",
                "[\"first_name\", \"last_name\", \"degree\", \"status\", \"year\", \"average\", \"ssn\"]"
                );

            var jobCertSchemaId = await CreateSchema(govInfo.DidEntity.Wallet, govInfo.DidEntity.DidInfo.Did,
                "Job-Certificate",
                "0.1",
                "[\"first_name\", \"last_name\", \"salary\", \"employee_status\", \"experience\"]"
                );

            // Step 6: Credential Definition Setup
            // faber fetches transcript schema from ledger and publishes a schema def
            await RetrieveSchemaAndPublishDef(faberInfo.DidEntity, govInfo.DidEntity, transcriptSchemaId);
            await RetrieveSchemaAndPublishDef(acmeInfo.DidEntity, govInfo.DidEntity, jobCertSchemaId);

            // acme fetches job cert schema from ledger and publish a schema def

        }

        private async Task<string> RetrieveSchemaAndPublishDef(DidEntity publisher, DidEntity originalIssuer, string transcriptSchemaId)
        {
            var getSchemaRequest = await Ledger.BuildGetSchemaRequestAsync(originalIssuer.DidInfo.Did, transcriptSchemaId);
            var getSchemaResponse = await Ledger.SubmitRequestAsync(pool, getSchemaRequest);
            var parsedGetSchemaResponse = await Ledger.ParseGetSchemaResponseAsync(getSchemaResponse);
            var claimDef = await AnonCreds.IssuerCreateAndStoreCredentialDefAsync(publisher.Wallet,
                publisher.DidInfo.Did,
                parsedGetSchemaResponse.ObjectJson,
                "TAG1",
                SignatureType.CL,
                "{\"support_revocation\": false}");
            var createCredDefRequest = await Ledger.BuildCredDefRequestAsync(publisher.DidInfo.Did, claimDef.CredDefJson);
            var createCredDefResponse = await Ledger.SignAndSubmitRequestAsync(pool, publisher.Wallet, publisher.DidInfo.Did, createCredDefRequest);
            return createCredDefResponse;
        }

        private async Task<string> CreateSchema(Wallet issuerWallet, string issuerDid, string name, string version, string attrs)
        {
            var schema = await AnonCreds.IssuerCreateSchemaAsync(issuerDid, name, version, attrs);
            string schemaRequest = await Ledger.BuildSchemaRequestAsync(issuerDid, schema.SchemaJson);
            var throwAway = await Ledger.SignAndSubmitRequestAsync(pool, issuerWallet, issuerDid, schemaRequest);
            return schema.SchemaId;
        }

        private async Task<OnboardingDetail> OnboardTrustee(string trustee, DidEntity stewardDidEntity)
        {
            // a. Connecting the Establishment. 

            // Trustee and steward contact in some way. Can be filling the form on a Steward's web site or a phone call

            // ## steward side
            // create pair wise identifier for steward to trustee
            var stewardtrusteeDidResult = await Did.CreateAndStoreMyDidAsync(stewardDidEntity.Wallet, "{}");
            var stewardtrusteeDid = new DidInfo()
            {
                Did = stewardtrusteeDidResult.Did,
                VerKey = stewardtrusteeDidResult.VerKey
            };

            var nymRequest = await Ledger.BuildNymRequestAsync(stewardDidEntity.DidInfo.Did, stewardtrusteeDid.Did, stewardtrusteeDid.VerKey, null, null);
            await Ledger.SignAndSubmitRequestAsync(pool, stewardDidEntity.Wallet, stewardDidEntity.DidInfo.Did, nymRequest);

            // create connection request for trustee college
            var connectionRequest = new ConnectionRequest()
            {
                Did = stewardtrusteeDid.Did,
                Nonce = 123456789
            };

            // pretend we sent the connectio request to trustee

            // ## trustee side
            // trustee create pair wise identifier 
            await Wallet.CreateWalletAsync(poolName, $"{trustee}_wallet", null, null, null);
            var trusteeWallet = await Wallet.OpenWalletAsync($"{trustee}_wallet", null, null);
            var trusteeStewardDidResult = await Did.CreateAndStoreMyDidAsync(trusteeWallet, "{}");
            var trusteeStewardDidInfo = new DidInfo()
            {
                Did = trusteeStewardDidResult.Did,
                VerKey = trusteeStewardDidResult.VerKey
            };

            var connectionResponse = new ConnectionResponse()
            {
                Did = trusteeStewardDidInfo.Did,
                VerKey = trusteeStewardDidInfo.VerKey,
                Nonce = connectionRequest.Nonce
            };

            // ask ledger for steward trustee verKey
            var stewardTrusteeVerKey = await Did.KeyForDidAsync(pool, trusteeWallet, connectionRequest.Did);
            // encrypt the response
            var anonEncryptedConnectionResponse = await Crypto.AnonCryptAsync(stewardTrusteeVerKey, connectionResponse.ToJsonThenBytes());

            // pretend we sent the connection response to steward

            // ## steward side
            // decrypt the response
            var decryptedConnectionResponse = (await Crypto.AnonDecryptAsync(stewardDidEntity.Wallet, stewardtrusteeDid.VerKey, anonEncryptedConnectionResponse)).Serialize<ConnectionResponse>();

            if (connectionRequest.Nonce != decryptedConnectionResponse.Nonce)
            {
                throw new Exception("Nonce not matched");
            }

            // create pair wise identifier for trustee to steward
            var trusteeStewardDidNymRequest = await Ledger.BuildNymRequestAsync(stewardDidEntity.DidInfo.Did, decryptedConnectionResponse.Did, decryptedConnectionResponse.VerKey, null, null);
            await Ledger.SignAndSubmitRequestAsync(pool, stewardDidEntity.Wallet, stewardDidEntity.DidInfo.Did, trusteeStewardDidNymRequest);


            // b. trustee getting Verinym. Steward creating trustee's did on trustee's behalf

            // trustee side
            var trusteeDid = await Did.CreateAndStoreMyDidAsync(trusteeWallet, "{}");

            var trusteeDidInfo = new DidInfo()
            {
                Did = trusteeDid.Did,
                VerKey = trusteeDid.VerKey
            };

            var createDidRequestMessage = new Message()
            {
                SenderDidInfo = trusteeStewardDidInfo,
                Payload = trusteeDidInfo.ToJson()
            };

            // trustee authenticates and encrypts the message by calling crypto.auth_crypt using verkeys created for secure communication with Steward.
            // The Authenticated-encryption schema is designed for the sending of a confidential message specifically for a Recipient, using the 
            // Sender's public key. Using the Recipient's public key, the Sender can compute a shared secret key. Using the Sender's public key and
            // his secret key, the Recipient can compute the exact same shared secret key. That shared secret key can be used to verify that the encrypted 
            // message was not tampered with, before eventually decrypting it.
            var authCryptedTrusteeDidInfoJson = await Crypto.AuthCryptAsync(trusteeWallet, trusteeStewardDidInfo.VerKey, stewardtrusteeDid.VerKey, createDidRequestMessage.ToJsonThenBytes());

            // pretend trustee sent the message to stewardc

            // steward side

            // decrypt the message with private key
            var decryptedResult = await Crypto.AuthDecryptAsync(stewardDidEntity.Wallet, stewardTrusteeVerKey, authCryptedTrusteeDidInfoJson);
            var decryptedMessage = decryptedResult.MessageData.Serialize<Message>();


            // authenticate by looking up trusteeStewardVerKey on ledger and compare with the one in message
            var trusteeStewardVerKey = await Did.KeyForDidAsync(pool, stewardDidEntity.Wallet, decryptedMessage.SenderDidInfo.Did);
            if (decryptedResult.TheirVk != trusteeStewardVerKey)
            {
                throw new Exception("Nonce not matched");
            }

            var decryptedtrusteeDidInfo = JsonConvert.DeserializeObject<DidInfo>(decryptedMessage.Payload);
            var trusteeDidNymRequest = await Ledger.BuildNymRequestAsync(stewardDidEntity.DidInfo.Did, decryptedtrusteeDidInfo.Did, decryptedtrusteeDidInfo.VerKey, null, "TRUST_ANCHOR");
            await Ledger.SignAndSubmitRequestAsync(pool, stewardDidEntity.Wallet, stewardDidEntity.DidInfo.Did, trusteeDidNymRequest);

            

            // At this point, trustee has a DID related to his identity in the Ledger.

            var onboardingDetail = new OnboardingDetail
            {
                StewardTrusteeDidInfo = stewardtrusteeDid,
                TrusteeStewardDidInfo = trusteeStewardDidInfo,
                DidEntity = new DidEntity()
                {
                    DidInfo = trusteeDidInfo,
                    Wallet = trusteeWallet
                }
            };

            return onboardingDetail;
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
            return await Pool.OpenPoolLedgerAsync(poolName, openPoolConfigurationJSON);
        }

        private async Task<DidEntity> RestoreSteward()
        {
            string stewardWalletName = "sovrin_steward_wallet"+ Guid.NewGuid().ToString();
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

    public class OnboardingDetail
    {
        public DidInfo StewardTrusteeDidInfo { get; set; }
        public DidInfo TrusteeStewardDidInfo { get; set; }
        public DidEntity DidEntity { get; set; }
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

        public static T Serialize<T>(this string str)
        {
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
