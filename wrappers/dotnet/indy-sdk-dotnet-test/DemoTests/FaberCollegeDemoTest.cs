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
    public class FaberCollegeDemoTest : IndyIntegrationTestBase
    {
        JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

        private readonly string poolName = $"pool-{Guid.NewGuid()}".ToLower();

        Pool poolHandle;

        [TestMethod]
        public async Task RunFaberCollegeDemo()
        {
            await CreatePool(poolName);

            DidEntity steward = await CreateSteward();

            DidEntity faber = await CreateAndOnboardTrustee("faber", steward.Wallet, steward.DidInfo.Did);

            DidEntity acme = await CreateAndOnboardTrustee("acme", steward.Wallet, steward.DidInfo.Did);

            DidEntity government = await CreateAndOnboardTrustee("government", steward.Wallet, steward.DidInfo.Did);

            DidEntity thriftBank = await CreateAndOnboardTrustee("thrift_bank", steward.Wallet, steward.DidInfo.Did);

            // The Trust Anchor optionally creates a new DID record in his wallet and sends the corresponding NYM transaction to the Ledger.

            DidInfo governmentIssuer = await CreatePairwiseDid(government);

            // The Trust Anchor prepares the Credential Schema.

            CredentialSchema transcriptCredentialSchema = new CredentialSchema
            {
                Name = "Transcript",
                Version = "1.2",
                AttributeNames = new string[] { "first_name", "last_name", "degree", "status", "year", "average", "ssn" }
            };

            string transcriptSchemaRequest = await Ledger.BuildSchemaRequestAsync(governmentIssuer.Did, SerializeToJSON(transcriptCredentialSchema));

            await Ledger.SignAndSubmitRequestAsync(poolHandle, government.Wallet, governmentIssuer.Did, transcriptSchemaRequest);

            CredentialSchema jobCertificateCredentialSchema = new CredentialSchema
            {
                Name = "Job-Certificate",
                Version = "0.1",
                AttributeNames = new string[] { "first_name", "last_name", "salary", "employee_status", "experience" }
            };

            string jobCertificateSchemaRequest = await Ledger.BuildSchemaRequestAsync(governmentIssuer.Did, SerializeToJSON(jobCertificateCredentialSchema));

            await Ledger.SignAndSubmitRequestAsync(poolHandle, government.Wallet, governmentIssuer.Did, jobCertificateSchemaRequest);


            // A Credential Definition can be created and saved in the Ledger by any Trust Anchor. 

            // Here Faber creates and publishes a Credential Definition for the known Transcript Credential Schema to the Ledger.

            // The Trust Anchor optionally creates new DID record in his wallet and sends the corresponding NYM transaction to the Ledger.

            DidInfo faberIssuer = await CreatePairwiseDid(faber);

            DidInfo acmeIssuer = await CreatePairwiseDid(acme);

            CredentialSchemaBase faberTranscriptSchemaBase = new CredentialSchemaBase
            {
                Name = transcriptCredentialSchema.Name,
                Version = transcriptCredentialSchema.Version
            };

            CredentialSchemaBase acmeJobCertificateSchemaBase = new CredentialSchemaBase
            {
                Name = jobCertificateCredentialSchema.Name,
                Version = jobCertificateCredentialSchema.Version
            };

            CredentialSchemaResult faberTranscriptSchema = await CreateAndPublishCredentialDefinition(faberTranscriptSchemaBase, governmentIssuer, faber, faberIssuer);

            CredentialSchemaResult acmeJobCertificateSchema = await CreateAndPublishCredentialDefinition(acmeJobCertificateSchemaBase, governmentIssuer, acme, acmeIssuer);

            DidEntity alice = await CreateDidEntity("alice", faberIssuer);

            DidInfo aliceFaber = await CreateDidForSecuredConnection(alice, faber);

            Console.WriteLine("==============================");
            Console.WriteLine("== Getting Transcript with Faber - Getting Transcript Claim ==");
            Console.WriteLine("------------------------------");

            Console.WriteLine("\"Faber\" -> Create \"Transcript\" Claim Offer for Alice");
            string transcriptClaimOfferJSON = await AnonCreds.IssuerCreateClaimOfferAsync(faber.Wallet, SerializeToJSON(faberTranscriptSchema), faberIssuer.Did, aliceFaber.Did);

            Console.WriteLine("\"Faber\" -> Get key for Alice did");
            string aliceFaberVerKey = await Did.KeyForDidAsync(poolHandle, faber.Wallet, aliceFaber.Did);

            Console.WriteLine("\"Faber\" -> Authcrypt \"Transcript\" Claim Offer for Alice");
            var authCryptedTranscriptClaimOffer = await Crypto.AuthCryptAsync(faber.Wallet, faber.DidInfo.VerKey, aliceFaberVerKey, Encoding.UTF8.GetBytes(transcriptClaimOfferJSON));

            Console.WriteLine("\"Faber\" -> Send authcrypted \"Transcript\" Claim Offer to Alice");
            Console.WriteLine("\"Alice\" -> Authdecrypted \"Transcript\" Claim Offer from Faber");
            AuthDecryptResult authDecryptedTranscriptOfferClaim = await Crypto.AuthDecryptAsync(alice.Wallet, aliceFaber.VerKey, authCryptedTranscriptClaimOffer);

            string authDecryptedTranscriptOfferClaimJSON = Encoding.UTF8.GetString(authDecryptedTranscriptOfferClaim.MessageData);

            ClaimOfferResponse transcriptClaimOffer = JsonConvert.DeserializeObject<ClaimOfferResponse>(authDecryptedTranscriptOfferClaimJSON);

            await AnonCreds.ProverStoreClaimOfferAsync(alice.Wallet, authDecryptedTranscriptOfferClaimJSON);

            CredentialSchemaResult aliceTranscriptSchema = await RequestCredentialSchemaResult(transcriptClaimOffer, alice.DidInfo);

            Assert.AreEqual(transcriptCredentialSchema.Name, aliceTranscriptSchema.CredentialSchema.Name);

            Assert.AreEqual(transcriptCredentialSchema.Version, aliceTranscriptSchema.CredentialSchema.Version);

            Console.WriteLine("\"Alice\" -> Create and store \"Alice\" Master Secret in Wallet");
            string aliceMasterSecret = "alice_master_secret";
            await AnonCreds.ProverCreateMasterSecretAsync(alice.Wallet, aliceMasterSecret);

            Console.WriteLine("\"Alice\" -> Get \"Faber Transcript\" Claim Definition from Ledger");
            string faberTranscriptClaimDef = await Ledger.BuildGetClaimDefTxnAsync(aliceFaber.Did, faberTranscriptSchema.SequenceNumber.Value, SignatureType.CL, faberIssuer.Did);
            string faberTranscriptClaimDefJSON = await Ledger.SubmitRequestAsync(poolHandle, faberTranscriptClaimDef);
            LedgerResult<ClaimDefinitionResult> claimResponse = JsonConvert.DeserializeObject<LedgerResult<ClaimDefinitionResult>>(faberTranscriptClaimDefJSON);
            ClaimDefinitionResult aliceTranscriptClaimDef = claimResponse.Result;

            string faberAliceKey = faber.DidInfo.VerKey;


            Console.WriteLine("\"Alice\" -> Create and store in Wallet \"Transcript\" Claim Request for Faber");
            string aliceTranscriptClaimDefJSON = SerializeToJSON(aliceTranscriptClaimDef);
            string transcriptClaimRequestJSON = await AnonCreds.ProverCreateAndStoreClaimReqAsync(alice.Wallet, aliceFaber.Did, authDecryptedTranscriptOfferClaimJSON, aliceTranscriptClaimDefJSON, aliceMasterSecret);

            Console.WriteLine("\"Alice\" -> Authcrypt \"Transcript\" Claim Request for Faber");
            var authCryptedTranscriptClaimRequestByte = await Crypto.AuthCryptAsync(alice.Wallet, aliceFaber.VerKey, faber.DidInfo.VerKey, Encoding.UTF8.GetBytes(transcriptClaimRequestJSON));

            Console.WriteLine("\"Alice\" -> Send authcrypted \"Transcript\" Claim Request to Faber");
            Console.WriteLine("\"Faber\" -> Authdecrypt \"Transcript\" Claim Request from Alice");
            AuthDecryptResult authDecryptedTranscriptClaimRequest = await Crypto.AuthDecryptAsync(faber.Wallet, faber.DidInfo.VerKey, authCryptedTranscriptClaimRequestByte);
            string authDecryptedTranscriptClaimRequestJSON = Encoding.UTF8.GetString(authDecryptedTranscriptClaimRequest.MessageData);



            Console.WriteLine("\"Faber\" -> Create \"Transcript\" Claim for Alice");
            Dictionary<string, string> claimKeyValuePair = transcriptClaimOffer.KeyCorrectnessProof.ClaimKeyValuePair;
            Dictionary<string, string[]> transcriptClaimValues = new Dictionary<string, string[]>
            {
                { "first_name",  new string[] { "Alice", claimKeyValuePair["first_name"] }},
                { "last_name",  new string[] { "Garcia", claimKeyValuePair["last_name"] }},
                { "degree",  new string[] { "Bachelor of Science, Marketing", claimKeyValuePair["degree"] }},
                { "status",  new string[] { "graduated", claimKeyValuePair["status"] }},
                { "ssn",  new string[] { "123-45-6789", claimKeyValuePair["ssn"] }},
                { "year",  new string[] { "2015", claimKeyValuePair["year"] }},
                { "average",  new string[] { "5", claimKeyValuePair["average"] }},
            };
            string transcriptClaimValuesJSON = SerializeToJSON(transcriptClaimValues);
            IssuerCreateClaimResult transcriptClaim = await AnonCreds.IssuerCreateClaimAsync(faber.Wallet, authDecryptedTranscriptClaimRequestJSON, transcriptClaimValuesJSON, -1);


            Console.WriteLine("\"Faber\" -> Authcrypt \"Transcript\" Claim for Alice");
            var authCryptedTranscriptClaimByte = await Crypto.AuthCryptAsync(faber.Wallet, faberAliceKey, aliceFaberVerKey, Encoding.UTF8.GetBytes(transcriptClaim.ClaimJson));
            string authCryptedTranscriptClaimJSON = Encoding.UTF8.GetString(authCryptedTranscriptClaimByte);

            Console.WriteLine("\"Faber\" -> Send authcrypted \"Transcript\" Claim to Alice");
            Console.WriteLine("\"Alice\" -> Authdecrypted \"Transcript\" Claim from Faber");
            AuthDecryptResult authDecryptedTranscriptClaim = await Crypto.AuthDecryptAsync(alice.Wallet, aliceFaber.VerKey, authCryptedTranscriptClaimByte);


            Console.WriteLine("\"Alice\" -> Store \"Transcript\" Claim from Faber");
            string authDecryptedTranscriptClaimJSON = Encoding.UTF8.GetString(authDecryptedTranscriptClaim.MessageData);
            await AnonCreds.ProverStoreClaimAsync(alice.Wallet, authDecryptedTranscriptClaimJSON, null);

            string proverClaimsJSON = await AnonCreds.ProverGetClaimsAsync(alice.Wallet, "{}");
            ProverClaims[] proverClaims = JsonConvert.DeserializeObject<ProverClaims[]>(proverClaimsJSON);
            Assert.IsTrue(proverClaims.Length > 0);
            Assert.IsTrue(proverClaims.First().Attrs.Count > 0);
            Assert.IsTrue(proverClaims.First().Attrs["first_name"] == "Alice");


            Console.WriteLine("==============================");
            Console.WriteLine("=== Apply for the job with Acme ==");
            Console.WriteLine("==============================");
            Console.WriteLine("== Apply for the job with Acme - Onboarding ==");
            Console.WriteLine("------------------------------");

            DidInfo aliceAcme = await CreateDidForSecuredConnection(alice, acme);
            string acmeAliceVerKey = acme.DidInfo.VerKey;
            string aliceAcmeVerKey = aliceAcme.VerKey;
            Console.WriteLine("\"Faber\" -> Get key for Alice did");
            string aliceAcmeVerKey__ = await Did.KeyForDidAsync(poolHandle, acme.Wallet, aliceAcme.Did);

            SchemaKey transcriptSchemaKey = proverClaims.First().SchemaKey;

            SchemaProofRequest proofRequest = new SchemaProofRequest
            {
                Name = acmeJobCertificateSchemaBase.Name,
                Version = acmeJobCertificateSchemaBase.Version,
                Nonce = "1432422343242122312411212",
                RequestedAttributes = new Dictionary<string, RequestedSchemaAttributes>
                {
                    {  "attr1_referent", new RequestedSchemaAttributes { Name = "first_name" }},
                    {  "attr2_referent", new RequestedSchemaAttributes { Name = "last_name" }},
                    {  "attr3_referent", new RequestedSchemaAttributes { Name = "degree" , Restrictions = new Restriction[] { new Restriction { IssuerDid = faberIssuer.Did, SchemaKey = transcriptSchemaKey }}}},
                    {  "attr4_referent", new RequestedSchemaAttributes { Name = "status" , Restrictions = new Restriction[] { new Restriction { IssuerDid = faberIssuer.Did, SchemaKey = transcriptSchemaKey }}}},
                    {  "attr5_referent", new RequestedSchemaAttributes { Name = "ssn" , Restrictions = new Restriction[] { new Restriction { IssuerDid = faberIssuer.Did, SchemaKey = transcriptSchemaKey }}}},
                    {  "attr6_referent", new RequestedSchemaAttributes { Name = "phone_number",  }},
                },
                RequestedPredicates = new Dictionary<string, PredicateReferent>
                {
                    { "predicate1_referent", new PredicateReferent { AttrName = "average", PType = ">=", Value = 4, Restrictions = new Restriction[] { new Restriction { IssuerDid = faberIssuer.Did, SchemaKey = transcriptSchemaKey }}}}
                }
            };

            string jobApplicationProofRequestJSON = SerializeToJSON(proofRequest);

            Console.WriteLine("\"Acme\" -> Authcrypt \"Job-Application\" Proof Request for Alice");
            byte[] authCryptedJobApplicationProofRequest = await Crypto.AuthCryptAsync(acme.Wallet, acmeAliceVerKey, aliceAcmeVerKey, Encoding.UTF8.GetBytes(jobApplicationProofRequestJSON));

            Console.WriteLine("\"Acme\" -> Send authcrypted \"Job-Application\" Proof Request to Alice");
            Console.WriteLine("\"Alice\" -> Authdecrypt \"Job-Application\" Proof Request from Acme");
            AuthDecryptResult authDecryptedJobApplicationProofRequest = await Crypto.AuthDecryptAsync(alice.Wallet, aliceAcme.VerKey, authCryptedJobApplicationProofRequest);

            Console.WriteLine("\"Alice\" -> Get claims for \"Job-Application\" Proof Request");
            string authDecryptedJobApplicationProofRequestJSON = Encoding.UTF8.GetString(authDecryptedJobApplicationProofRequest.MessageData);
            string claimsForJobApplicationProofRequest = await AnonCreds.ProverGetClaimsForProofReqAsync(alice.Wallet, authDecryptedJobApplicationProofRequestJSON);
            ClaimsForProofResponse claimsForProof = JsonConvert.DeserializeObject<ClaimsForProofResponse>(claimsForJobApplicationProofRequest);
            Dictionary<string, Referent[]> attributesForClaims = claimsForProof.Attrs;
            Dictionary<string, Referent[]> predicatesForClaims = claimsForProof.Predicates;
            Referent[] referents = attributesForClaims.SelectMany(x => x.Value).Union(predicatesForClaims.SelectMany(y => y.Value)).ToArray();
            Referent attr3 = attributesForClaims["attr3_referent"].First();
            Referent attr4 = attributesForClaims["attr4_referent"].First();
            Referent attr5 = attributesForClaims["attr5_referent"].First();
            Referent predicate = predicatesForClaims["predicate1_referent"].First();


            Console.WriteLine("\"Alice\" -> Create \"Job-Application\" Proof");
            ProofRequest jobApplicationRequestedClaims = new ProofRequest
            {
                SelfAttestedAttributes = new Dictionary<string, string>
                {
                    { "attr1_referent", "Alice" },
                    { "attr2_referent", "Garcia" },
                    { "attr6_referent", "123-45-6789" }

                },
                RequestedAttrs = new Dictionary<string, object[]>
                {
                    { "attr3_referent", new object[] { attr3.ClaimUUID, true } },
                    { "attr4_referent", new object [] { attr4.ClaimUUID, true } } ,
                    { "attr5_referent", new object[] { attr5.ClaimUUID, true } },
                },
                RequestedPredicates = new Dictionary<string, string>
                {
                    {"predicate1_referent", predicate.ClaimUUID }
                }
            };



            (Dictionary<string, CredentialSchemaResult> schemas, Dictionary<string, ClaimDefinitionResult> claims) entities = await GetEntitiesFromLedgerAsync(aliceFaber, referents);

            string jobApplicationRequestedClaimsJSON = SerializeToJSON(jobApplicationRequestedClaims);
            string revocRegsJSON = "{}";
            string schemasJSON = SerializeToJSON(entities.schemas);
            string claimsDefJSON = SerializeToJSON(entities.claims);

            string jobApplicationProofJSON = await AnonCreds.ProverCreateProofAsync(alice.Wallet, authDecryptedJobApplicationProofRequestJSON, jobApplicationRequestedClaimsJSON, schemasJSON, aliceMasterSecret, claimsDefJSON, revocRegsJSON);

        }

        private async Task<(Dictionary<string, CredentialSchemaResult> schemas, Dictionary<string, ClaimDefinitionResult> claims)> GetEntitiesFromLedgerAsync(DidInfo requester, Referent[] items)
        {
            Dictionary<string, CredentialSchemaResult> schemas = new Dictionary<string, CredentialSchemaResult>();
            Dictionary<string, ClaimDefinitionResult> claims = new Dictionary<string, ClaimDefinitionResult>();


            await Task.WhenAll(items.Select(async item =>
            {
                CredentialSchemaResult receivedSchema = await GetSchemaAsync(requester, item.SchemaKey);
                schemas.TryAdd(item.ClaimUUID, receivedSchema);

                ClaimDefinitionResult receivedClaims = await GetClaimDefAsync(requester, receivedSchema, item.IssuerDid);
                claims.TryAdd(item.ClaimUUID, receivedClaims);
            }));

            return (schemas, claims);
        }

        private async Task<CredentialSchemaResult> RequestCredentialSchemaResult(ClaimOfferResponse claim, DidInfo requester)
        {
            string schemaRequestJSON = SerializeToJSON(new CredentialSchemaBase { Name = claim.SchemaKey.Name, Version = claim.SchemaKey.Version });

            string ledgerSchemaRequest = await Ledger.BuildGetSchemaRequestAsync(requester.Did, claim.IssuerDid, schemaRequestJSON);

            string ledgerSchemaResponseJSON = await Ledger.SubmitRequestAsync(poolHandle, ledgerSchemaRequest);

            LedgerResult<CredentialSchemaResult> ledgerSchemaResponse = JsonConvert.DeserializeObject<LedgerResult<CredentialSchemaResult>>(ledgerSchemaResponseJSON);

            return ledgerSchemaResponse.Result;
        }

        private async Task<CredentialSchemaResult> GetSchemaAsync(DidInfo requester, SchemaKey schemaKey)
        {
            var getSchemaData = SerializeToJSON(new CredentialSchemaBase { Name = schemaKey.Name, Version = schemaKey.Version });
            var getSchemaRequest = await Ledger.BuildGetSchemaRequestAsync(requester.Did, schemaKey.Did, getSchemaData);
            var getSchemaResponse = await Ledger.SubmitRequestAsync(poolHandle, getSchemaRequest);
            LedgerResult<CredentialSchemaResult> ledgerSchemaResponse = JsonConvert.DeserializeObject<LedgerResult<CredentialSchemaResult>>(getSchemaResponse);
            return ledgerSchemaResponse.Result;
        }

        private async Task<ClaimDefinitionResult> GetClaimDefAsync(DidInfo requester, CredentialSchemaResult schemaResult, string issuerDid)
        {
            string getClaimDefRequest = await Ledger.BuildGetClaimDefTxnAsync(requester.Did, schemaResult.SequenceNumber.Value, SignatureType.CL, issuerDid);
            string claimString = await Ledger.SubmitRequestAsync(poolHandle, getClaimDefRequest);
            LedgerResult<ClaimDefinitionResult> claimResponse = JsonConvert.DeserializeObject<LedgerResult<ClaimDefinitionResult>>(claimString);

            return claimResponse.Result;
        }


        private async Task<CredentialSchemaResult> CreateAndPublishCredentialDefinition(CredentialSchemaBase getSchema, DidInfo fromDid, DidEntity toEntity, DidInfo toEntityIssuer)
        {
            string ledgerSchemaRequest = await Ledger.BuildGetSchemaRequestAsync(toEntityIssuer.Did, fromDid.Did, SerializeToJSON(getSchema));

            string ledgerSchemaResponseJSON = await Ledger.SubmitRequestAsync(poolHandle, ledgerSchemaRequest);

            LedgerResult<CredentialSchemaResult> ledgerSchemaResponse = JsonConvert.DeserializeObject<LedgerResult<CredentialSchemaResult>>(ledgerSchemaResponseJSON);

            string claimSchemaJSON = SerializeToJSON(ledgerSchemaResponse.Result);

            string faberTranscriptClaimDefJSON = await AnonCreds.IssuerCreateAndStoreClaimDefAsync(toEntity.Wallet, toEntityIssuer.Did, claimSchemaJSON, SignatureType.CL, false);

            ClaimDefinitionResult faberTranscriptClaimDef = JsonConvert.DeserializeObject<ClaimDefinitionResult>(faberTranscriptClaimDefJSON);

            string faberTranscriptClaimDataJSON = SerializeToJSON(faberTranscriptClaimDef.Data);

            string claimDefLedgerRequest = await Ledger.BuildClaimDefTxnAsync(toEntityIssuer.Did, faberTranscriptClaimDef.Ref, faberTranscriptClaimDef.SignatureType, faberTranscriptClaimDataJSON);

            await Ledger.SignAndSubmitRequestAsync(poolHandle, toEntity.Wallet, toEntityIssuer.Did, claimDefLedgerRequest);

            return ledgerSchemaResponse.Result;
        }

        private async Task<DidInfo> CreatePairwiseDid(DidEntity entity)
        {
            CreateAndStoreMyDidResult identity = await Did.CreateAndStoreMyDidAsync(entity.Wallet, SerializeToJSON(new DidInfo()));

            string nymRequest = await Ledger.BuildNymRequestAsync(entity.DidInfo.Did, identity.Did, identity.VerKey, null, null);

            await Ledger.SignAndSubmitRequestAsync(poolHandle, entity.Wallet, entity.DidInfo.Did, nymRequest);

            return new DidInfo
            {
                Did = identity.Did,
                VerKey = identity.VerKey
            };
        }

        private async Task CreatePool(string poolName)
        {

            FileStream genesisTxnFile = PoolUtils.CreateGenesisTxnFile("genesis.txn");

            string PoolGenesisTxnPath = Path.GetFullPath(genesisTxnFile.Name).Replace('\\', '/');

            GenesisTransactionPath poolConfig = new GenesisTransactionPath { GenesisTransaction = PoolGenesisTxnPath };

            OpenPoolConfiguration openPoolConfiguration = new OpenPoolConfiguration { RefreshOnOpen = true };

            string poolConfigJSON = SerializeToJSON(poolConfig);

            string openPoolConfigurationJSON = SerializeToJSON(openPoolConfiguration);

            await Pool.CreatePoolLedgerConfigAsync(poolName, poolConfigJSON);

            poolHandle = await Pool.OpenPoolLedgerAsync(poolName, openPoolConfigurationJSON);
        }


        private async Task<DidEntity> CreateDidEntity(string identityName, DidInfo trustee, string seed = null)
        {
            string walletName = $"{identityName.Trim()}_wallet";

            await Wallet.CreateWalletAsync(poolName, walletName, null, null, null);

            Wallet identityWallet = await Wallet.OpenWalletAsync(walletName, null, null);

            string identityDidInfoJSON = SerializeToJSON(new DidInfo
            {
                Seed = seed
            });

            CreateAndStoreMyDidResult identityDidResult = await Did.CreateAndStoreMyDidAsync(identityWallet, identityDidInfoJSON);

            return new DidEntity
            {
                DidInfo = new DidInfo
                {
                    Did = identityDidResult.Did,
                    VerKey = identityDidResult.VerKey
                },
                Wallet = identityWallet
            };
        }


        private async Task<DidEntity> CreateSteward()
        {
            string stewardWalletName = "sovrin_steward_wallet";

            await Wallet.CreateWalletAsync(poolName, stewardWalletName, null, null, null);

            Wallet stewardWallet = await Wallet.OpenWalletAsync(stewardWalletName, null, null);

            DidInfo stewardDidInfo = new DidInfo
            {
                Seed = "000000000000000000000000Steward1"
            };

            string stewardDidInfoJSON = SerializeToJSON(stewardDidInfo);

            CreateAndStoreMyDidResult stewardDidResult = await Did.CreateAndStoreMyDidAsync(stewardWallet, stewardDidInfoJSON);

            string stewardDid = stewardDidResult.Did;

            string stewardKey = stewardDidResult.VerKey;

            return new DidEntity
            {
                DidInfo = new DidInfo
                {
                    Did = stewardDid,
                    VerKey = stewardKey
                },
                Wallet = stewardWallet
            };
        }

        private async Task<DidInfo> CreateDidForSecuredConnection(DidEntity didEntity, DidEntity trusteeDidEntity)
        {
            string trusteeDidEntityDidInfoJSON = SerializeToJSON(new DidInfo());

            CreateAndStoreMyDidResult trusteeDidEntityDidResult = await Did.CreateAndStoreMyDidAsync(trusteeDidEntity.Wallet, trusteeDidEntityDidInfoJSON);

            string nymRequest = await Ledger.BuildNymRequestAsync(trusteeDidEntity.DidInfo.Did, trusteeDidEntityDidResult.Did, trusteeDidEntityDidResult.VerKey, null, null);

            await Ledger.SignAndSubmitRequestAsync(poolHandle, trusteeDidEntity.Wallet, trusteeDidEntity.DidInfo.Did, nymRequest);

            ConnectionRequest connectionRequest = new ConnectionRequest
            {
                Did = trusteeDidEntityDidResult.Did,
                Nonce = 123456789
            };

            string didEntityTrusteeDidInfoJSON = SerializeToJSON(new DidInfo());

            CreateAndStoreMyDidResult didEntityTrusteeDidResult = await Did.CreateAndStoreMyDidAsync(didEntity.Wallet, didEntityTrusteeDidInfoJSON);

            ConnectionResponse connectionResponse = new ConnectionResponse
            {
                Did = didEntityTrusteeDidResult.Did,
                VerKey = didEntityTrusteeDidResult.VerKey,
                Nonce = connectionRequest.Nonce
            };

            string trusteeDidEntityVerKey = await Did.KeyForDidAsync(poolHandle, didEntity.Wallet, connectionRequest.Did);

            string connectionResponseJSON = SerializeToJSON(connectionResponse);

            byte[] encodedConnectionResponse = Encoding.UTF8.GetBytes(connectionResponseJSON);

            byte[] anonCryptedConnectionResponse = await Crypto.AnonCryptAsync(trusteeDidEntityVerKey, encodedConnectionResponse);

            byte[] decryptedConnectionResponseByte = await Crypto.AnonDecryptAsync(trusteeDidEntity.Wallet, trusteeDidEntityDidResult.VerKey, anonCryptedConnectionResponse);

            string decryptedConnectionResponseJSON = Encoding.UTF8.GetString(decryptedConnectionResponseByte);

            ConnectionResponse decryptedConnectionResponse = JsonConvert.DeserializeObject<ConnectionResponse>(decryptedConnectionResponseJSON);

            Assert.AreEqual(connectionRequest.Nonce, decryptedConnectionResponse.Nonce);

            nymRequest = await Ledger.BuildNymRequestAsync(trusteeDidEntity.DidInfo.Did, decryptedConnectionResponse.Did, decryptedConnectionResponse.VerKey, null, null);

            await Ledger.SignAndSubmitRequestAsync(poolHandle, trusteeDidEntity.Wallet, trusteeDidEntity.DidInfo.Did, nymRequest);

            return new DidInfo
            {
                Did = didEntityTrusteeDidResult.Did,
                VerKey = didEntityTrusteeDidResult.VerKey
            };
        }

        private async Task<DidEntity> CreateAndOnboardTrustee(string trustee, Wallet stewardWallet, string stewardDid)
        {
            string stewardTrusteeDidInfoJSON = SerializeToJSON(new DidInfo());

            CreateAndStoreMyDidResult stewardTrusteeDidResult = await Did.CreateAndStoreMyDidAsync(stewardWallet, stewardTrusteeDidInfoJSON);

            string stewardTrusteeDid = stewardTrusteeDidResult.Did;

            string stewardTrusteeKey = stewardTrusteeDidResult.VerKey;

            string nymRequest = await Ledger.BuildNymRequestAsync(stewardDid, stewardTrusteeDid, stewardTrusteeKey, null, null);

            await Ledger.SignAndSubmitRequestAsync(poolHandle, stewardWallet, stewardDid, nymRequest);

            ConnectionRequest connectionRequest = new ConnectionRequest
            {
                Did = stewardTrusteeDid,
                Nonce = 123456789
            };

            string trusteeWalletName = $"{trustee}_wallet";

            await Wallet.CreateWalletAsync(poolName, trusteeWalletName, null, null, null);

            Wallet trusteeWallet = await Wallet.OpenWalletAsync(trusteeWalletName, null, null);

            string trusteeStewardDidInfoJSON = SerializeToJSON(new DidInfo());

            CreateAndStoreMyDidResult trusteeStewardDidResult = await Did.CreateAndStoreMyDidAsync(trusteeWallet, trusteeStewardDidInfoJSON);

            string trusteeStewardDid = trusteeStewardDidResult.Did;

            string trusteeStewardKey = trusteeStewardDidResult.VerKey;

            ConnectionResponse connectionResponse = new ConnectionResponse
            {
                Did = trusteeStewardDid,
                VerKey = trusteeStewardKey,
                Nonce = connectionRequest.Nonce
            };

            string stewardTrusteeVerKey = await Did.KeyForDidAsync(poolHandle, trusteeWallet, connectionRequest.Did);

            string connectionResponseJSON = SerializeToJSON(connectionResponse);

            byte[] encodedConnectionResponse = Encoding.UTF8.GetBytes(connectionResponseJSON);

            byte[] anonCryptedConnectionResponse = await Crypto.AnonCryptAsync(stewardTrusteeVerKey, encodedConnectionResponse);

            byte[] decryptedConnectionResponseByte = await Crypto.AnonDecryptAsync(stewardWallet, stewardTrusteeKey, anonCryptedConnectionResponse);

            string decryptedConnectionResponseJSON = Encoding.UTF8.GetString(decryptedConnectionResponseByte);

            ConnectionResponse decryptedConnectionResponse = JsonConvert.DeserializeObject<ConnectionResponse>(decryptedConnectionResponseJSON);

            Assert.AreEqual(connectionRequest.Nonce, decryptedConnectionResponse.Nonce);

            nymRequest = await Ledger.BuildNymRequestAsync(stewardDid, decryptedConnectionResponse.Did, decryptedConnectionResponse.VerKey, null, null);

            await Ledger.SignAndSubmitRequestAsync(poolHandle, stewardWallet, stewardDid, nymRequest);

            string trusteeDidInfoJSON = SerializeToJSON(new DidInfo());

            CreateAndStoreMyDidResult trusteeDidResult = await Did.CreateAndStoreMyDidAsync(trusteeWallet, trusteeDidInfoJSON);

            string trusteeDid = trusteeDidResult.Did;

            string trusteeVerKey = trusteeDidResult.VerKey;

            string trusteeDidResultJSON = SerializeToJSON(trusteeDidResult);

            byte[] trusteeDidResultJSONEncoded = Encoding.UTF8.GetBytes(trusteeDidResultJSON);

            byte[] authCryptedTrusteeDidInfoJSONByte = await Crypto.AuthCryptAsync(trusteeWallet, trusteeStewardKey, stewardTrusteeKey, trusteeDidResultJSONEncoded);

            string authCryptedTrusteeDidInfoJSON = Encoding.UTF8.GetString(authCryptedTrusteeDidInfoJSONByte);

            AuthDecryptResult authDecryptedTrusteeResult = await Crypto.AuthDecryptAsync(stewardWallet, stewardTrusteeKey, authCryptedTrusteeDidInfoJSONByte);

            string authDecryptedTrusteeDidInfoJSON = Encoding.UTF8.GetString(authDecryptedTrusteeResult.MessageData);

            DidInfo authDecryptedTrusteeInfo = JsonConvert.DeserializeObject<DidInfo>(authDecryptedTrusteeDidInfoJSON);

            nymRequest = await Ledger.BuildNymRequestAsync(stewardDid, authDecryptedTrusteeInfo.Did, authDecryptedTrusteeInfo.VerKey, null, RoleType.TRUST_ANCHOR);

            await Ledger.SignAndSubmitRequestAsync(poolHandle, stewardWallet, stewardDid, nymRequest);

            string ledgerTrusteeVerKey = await Did.KeyForDidAsync(poolHandle, stewardWallet, trusteeDid);

            Assert.AreEqual(authDecryptedTrusteeInfo.VerKey, ledgerTrusteeVerKey);

            return new DidEntity
            {
                DidInfo = new DidInfo
                {
                    Did = trusteeDid,
                    VerKey = trusteeVerKey
                },
                Wallet = trusteeWallet
            };
        }

        private async Task<DidEntity> CreateEntityAndEstablishConnectionWithTrustee(string entityName, DidEntity trustree)
        {
            throw new NotImplementedException();
        }

        private string SerializeToJSON(object model)
        {
            return JsonConvert.SerializeObject(model, Formatting.Indented, JsonSerializerSettings);
        }
    }
}

