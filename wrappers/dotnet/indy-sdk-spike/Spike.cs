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
            Console.WriteLine("Connecting to the Indy Nodes Pool");
            pool = await CreatePool(poolName);

            // Step 3: Getting the ownership for Stewards's Verinym
            Console.WriteLine("Getting the ownership for Stewards's Verinym");
            var stewardEntity = await RestoreStewardFromAlreadyKnownSeed();

            // Step 4: Onboarding trustee, Acme, Thrift and Government by Steward
            Console.WriteLine("Onboarding trustee, Acme, Thrift and Government by Steward");
            var faberOnboardingDetail = await OnboardNewEntity($"FaberColledge{Guid.NewGuid()}", stewardEntity);
            var acmeOnboardingDetail = await OnboardNewEntity($"AcmeCorp{Guid.NewGuid()}", stewardEntity);
            var thriftOnboardingDetail = await OnboardNewEntity($"ThriftBank{Guid.NewGuid()}", stewardEntity);
            var govOnboardingDetail = await OnboardNewEntity($"Government{Guid.NewGuid()}", stewardEntity);

            var faberEntity = faberOnboardingDetail.DidEntity;
            var acmeEntity = acmeOnboardingDetail.DidEntity;
            var thriftEntity = thriftOnboardingDetail.DidEntity;
            var govEntity = govOnboardingDetail.DidEntity;

            // Step 5: Credential Schema Setup
            Console.WriteLine("Credential Schema Setup");
            // trustee (government) create and publish credential schema
            var transcriptSchemaId = await CreateSchema(govEntity.Wallet, govEntity.DidInfo.Did,
                "Transcript",
                "1.2",
                "[\"first_name\", \"last_name\", \"degree\", \"status\", \"year\", \"average\", \"ssn\"]"
                );

            var jobCertSchemaId = await CreateSchema(govEntity.Wallet, govEntity.DidInfo.Did,
                "Job-Certificate",
                "0.1",
                "[\"first_name\", \"last_name\", \"salary\", \"employee_status\", \"experience\"]"
                );

            // Step 6: Credential Definition Setup
            Console.WriteLine("Credential Definition Setup");
            // faber fetches transcript schema from ledger and publishes a schema def
            var faberTransacriptCredDefId = await RetrieveSchemaAndPublishDef(faberEntity, govEntity, transcriptSchemaId);

            // acme fetches job cert schema from ledger and publish a schema def
            var acmeJobCertCredDefId = await RetrieveSchemaAndPublishDef(acmeEntity, govEntity, jobCertSchemaId);

            // alice gets a transcript

            // faber agent
            var transcriptCredOfferJson = await AnonCreds.IssuerCreateCredentialOfferAsync(faberEntity.Wallet, faberTransacriptCredDefId);

            // alice agent
            // TODO: NYM txn might be not needed for alice's did
            var aliceOnboardingDetail = await OnboardNewEntity($"alice{Guid.NewGuid()}", faberEntity);
            var aliceEntity = aliceOnboardingDetail.DidEntity;

            var aliceMasterSecretId = await AnonCreds.ProverCreateMasterSecretAsync(aliceEntity.Wallet, null);

            // alice get faber's schema
            var getSchemaRequest = await Ledger.BuildGetSchemaRequestAsync(aliceOnboardingDetail.TrusteeStewardDidInfo.Did, transcriptSchemaId);
            var getSchemaResponse = await Ledger.SubmitRequestAsync(pool, getSchemaRequest);
            var schemaResult = getSchemaResponse.Serialize<LedgerResult<CredentialSchemaResult>>();
            var transcriptSchema = await Ledger.ParseGetSchemaResponseAsync(getSchemaResponse);

            // alice get faber's def
            var getCredDefRequest = await Ledger.BuildGetCredDefRequestAsync(aliceOnboardingDetail.TrusteeStewardDidInfo.Did, faberTransacriptCredDefId);
            var getCredDefResponse = await Ledger.SubmitRequestAsync(pool, getCredDefRequest);
            var defResult = getCredDefResponse.Serialize<LedgerResult<ClaimDefinitionResult>>();
            var faberTranscriptCredDef = await Ledger.ParseGetCredDefResponseAsync(getCredDefResponse);

            // alice create a transcript request for faber
            var transcriptRequest = await AnonCreds.ProverCreateCredentialReqAsync(aliceEntity.Wallet,
                aliceOnboardingDetail.TrusteeStewardDidInfo.Did, transcriptCredOfferJson, faberTranscriptCredDef.ObjectJson, aliceMasterSecretId);

            // pretend alice agent sends the transcript request to faber

            // faber agent

            // faber now issues the claim for the request
            var transcriptCredValues = string.Empty;
            transcriptCredValues += "{";
            transcriptCredValues += "\"first_name\": {\"raw\": \"Alice\", \"encoded\": \"1139481716457488690172217916278103335\"},";
            transcriptCredValues += "\"last_name\": {\"raw\": \"Garcia\", \"encoded\": \"5321642780241790123587902456789123452\"},";
            transcriptCredValues += "\"degree\": {\"raw\": \"Bachelor of Science, Marketing\", \"encoded\": \"12434523576212321\"},";
            transcriptCredValues += "\"status\": {\"raw\": \"graduated\", \"encoded\": \"2213454313412354\"},";
            transcriptCredValues += "\"ssn\": {\"raw\": \"123-45-6789\", \"encoded\": \"3124141231422543541\"},";
            transcriptCredValues += "\"year\": {\"raw\": \"2015\", \"encoded\": \"2015\"},";
            transcriptCredValues += "\"average\": {\"raw\": \"5\", \"encoded\": \"5\"}";
            transcriptCredValues += "}";

            var createTranscriptCredResult = await AnonCreds.IssuerCreateCredentialAsync(faberEntity.Wallet,
                transcriptCredOfferJson, transcriptRequest.CredentialRequestJson, transcriptCredValues, null, null);

            // alice agent

            // alice store the credential in her wallet
            await AnonCreds.ProverStoreCredentialAsync(aliceEntity.Wallet,
                null,
                transcriptRequest.CredentialRequestMetadataJson,
                createTranscriptCredResult.CredentialJson,
                faberTranscriptCredDef.ObjectJson,
                null);



            // acme agent

            var jobApplicationProofRequestJson = GenerateAcmeProofRequest(faberTranscriptCredDef.Id);

            var aliceAcmeOnboarding = await OnboardNewEntity(aliceEntity.Wallet, acmeEntity, false);

            var authCryptedJObApplicationProofRequestJson = await Crypto.AuthCryptAsync(acmeEntity.Wallet,
                aliceAcmeOnboarding.StewardTrusteeDidInfo.VerKey, aliceAcmeOnboarding.TrusteeStewardDidInfo.VerKey, jobApplicationProofRequestJson.ToBytes());

            // alice agent
            var decryptedResultJobApplicationProofRequestJson = await AuthDecrypt(aliceEntity.Wallet, aliceAcmeOnboarding.TrusteeStewardDidInfo.VerKey, authCryptedJObApplicationProofRequestJson);
            

            // alice now prepare the proof for acme
            var claimForProofJson = await AnonCreds.ProverGetCredentialsForProofReqAsync(aliceEntity.Wallet, decryptedResultJobApplicationProofRequestJson);
            var claimForProof = claimForProofJson.Serialize<ClaimsForProofResponse>();
            var jobApplicationRequestedCredsJson = new
            {
                self_attested_attributes = new
                {
                    attr1_referent = "Alice",
                    attr2_referent = "Garcia",
                    attr6_referent = "123-45-6789"
                },
                requested_attributes = new
                {
                    attr3_referent = new { cred_id = claimForProof.Attrs["attr3_referent"].First().CredInfo.ClaimUUID, revealed = true },
                    attr4_referent = new { cred_id = claimForProof.Attrs["attr4_referent"].First().CredInfo.ClaimUUID, revealed = true },
                    attr5_referent = new { cred_id = claimForProof.Attrs["attr5_referent"].First().CredInfo.ClaimUUID, revealed = true }
                },
                requested_predicates = new
                {
                    predicate1_referent = new { cred_id = claimForProof.Predicates["predicate1_referent"].First().CredInfo.ClaimUUID }
                }
            }.ToJson();

            string requestedCred = System.IO.File.ReadAllText(@".\job_application_requested_creds_json.txt");
            requestedCred = requestedCred.Replace("cred_for_attr3", claimForProof.Attrs["attr3_referent"].First().CredInfo.ClaimUUID);
            requestedCred = requestedCred.Replace("cred_for_attr4", claimForProof.Attrs["attr4_referent"].First().CredInfo.ClaimUUID);
            requestedCred = requestedCred.Replace("cred_for_attr5", claimForProof.Attrs["attr5_referent"].First().CredInfo.ClaimUUID);
            requestedCred = requestedCred.Replace("cred_for_predicate1", claimForProof.Predicates["predicate1_referent"].First().CredInfo.ClaimUUID);


            var faberTranscriptCredDefJson = "{\"" + faberTranscriptCredDef.Id + "\":" + faberTranscriptCredDef.ObjectJson + "}";
            var transcriptSchemaJson = "{\"" + transcriptSchema.Id + "\":" + transcriptSchema.ObjectJson + "}";

            var applyJobProofJson = await AnonCreds.ProverCreateProofAsync(
                aliceEntity.Wallet,
                decryptedResultJobApplicationProofRequestJson, // TODO: perhaps change to decrypted version. DO IT
                requestedCred,
                aliceMasterSecretId,
                transcriptSchemaJson,
                faberTranscriptCredDefJson,
                "{}");

            var verified= await AnonCreds.VerifierVerifyProofAsync(decryptedResultJobApplicationProofRequestJson, applyJobProofJson, transcriptSchemaJson, faberTranscriptCredDefJson, "{}", "{}");
        }


        private static async Task<string> AuthDecrypt(Wallet myWallet, string myVk, byte[] message)
        {
            var decrypt = await Crypto.AuthDecryptAsync(myWallet, myVk, message);
            var obj = decrypt.MessageData.ToStringValue();
            return obj;
        }

        private static async Task<(string,string)> Onboarding(Pool pool, Wallet fromWallet, string fromDid, Wallet toWallet)
        {
            var didInfo =  await Did.CreateAndStoreMyDidAsync(fromWallet, "{}");
            await SendNym(pool, fromWallet, fromDid, didInfo.Did, didInfo.VerKey, null);
            string nonce = "123456789";
            return (didInfo.Did, nonce);
        }


        private static async Task SendNym(Pool pool, Wallet submitterWallet, string submitterDid, string newDid, string newKey, string role)
        {
            var nymRequest = await Ledger.BuildNymRequestAsync(submitterDid, newDid, newKey, null, role);
            await Ledger.SignAndSubmitRequestAsync(pool, submitterWallet, submitterDid, nymRequest);
        }
                    

        private static string GenerateAcmeProofRequest(string faberTranscriptCredDefId)
        {
            var proofRequest = new
            {
                nonce = "1432422343242122312411212",
                name = "Job-Application",
                version = "0.1",
                requested_attributes = new
                {
                    attr1_referent = new
                    {
                        name = "first_name"
                    },
                    attr2_referent = new
                    {
                        name = "last_name"
                    },
                    attr3_referent = new
                    {
                        name = "degree",
                        restrictions = new[] { new { cred_def_id = faberTranscriptCredDefId } }
                    },
                    attr4_referent = new
                    {
                        name = "status",
                        restrictions = new[] { new { cred_def_id = faberTranscriptCredDefId } }
                    },
                    attr5_referent = new
                    {
                        name = "ssn",
                        restrictions = new[] { new { cred_def_id = faberTranscriptCredDefId } }
                    },
                    attr6_referent = new
                    {
                        name = "phone_number"
                    }
                },
                requested_predicates = new
                {
                    predicate1_referent = new
                    {
                        name = "average",
                        p_type = ">=",
                        p_value = 4,
                        restrictions = new[] { new { cred_def_id = faberTranscriptCredDefId } }
                    }
                }
            };
            return proofRequest.ToJson();
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
            return claimDef.CredDefId;
        }

        private async Task<string> CreateSchema(Wallet issuerWallet, string issuerDid, string name, string version, string attrs)
        {
            var schema = await AnonCreds.IssuerCreateSchemaAsync(issuerDid, name, version, attrs);
            string schemaRequest = await Ledger.BuildSchemaRequestAsync(issuerDid, schema.SchemaJson);
            var throwAway = await Ledger.SignAndSubmitRequestAsync(pool, issuerWallet, issuerDid, schemaRequest);
            return schema.SchemaId;
        }


        private async Task<OnboardingDetail> OnboardNewEntity(string trustee, DidEntity stewardDidEntity, bool createTrusteeDid = true)
        {
            await Wallet.CreateWalletAsync(poolName, $"{trustee}_wallet", null, null, null);
            var trusteeWallet = await Wallet.OpenWalletAsync($"{trustee}_wallet", null, null);
            return await OnboardNewEntity(trusteeWallet, stewardDidEntity, createTrusteeDid);
        }


        private async Task<OnboardingDetail> OnboardNewEntity(Wallet trusteeWallet, DidEntity stewardDidEntity, bool createTrusteeDid = true)
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

            // create connection request for trustee
            var connectionRequest = new ConnectionRequest()
            {
                Did = stewardtrusteeDid.Did,
                Nonce = 123456789
            };

            // pretend we sent the connectio request to trustee

            // ## trustee side
            // trustee create pair wise identifier 

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

            var trusteeDidInfo = new DidInfo();

            if (createTrusteeDid)
            {
                var trusteeDid = await Did.CreateAndStoreMyDidAsync(trusteeWallet, "{}");
                trusteeDidInfo.Did = trusteeDid.Did;
                trusteeDidInfo.VerKey = trusteeDid.VerKey;

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
            }
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

        private async Task<DidEntity> RestoreStewardFromAlreadyKnownSeed()
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
            return JsonConvert.SerializeObject(model, Formatting.None, _jsonSerializerSettings);
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

        public static byte[] ToBytes(this string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return bytes;
        }

        public static string ToStringValue(this byte[] bytes)
        {
            var str = Encoding.UTF8.GetString(bytes);
            return str;
        }

    }
    
}
