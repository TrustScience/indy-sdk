using Hyperledger.Indy.WalletApi;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Hyperledger.Indy
{
    /// <summary>
    /// TODO
    /// </summary>
    public class IndySDKModels
    {
        /// <summary>
        /// TODO
        /// </summary>
        public class RoleType
        {
            /// <summary>
            /// TODO
            /// </summary>
            public static string STEWARD = "STEWARD";
            /// <summary>
            /// TODO
            /// </summary>
            public static string TRUSTEE = "TRUSTEE";
            /// <summary>
            /// TODO
            /// </summary>
            public static string TRUST_ANCHOR = "TRUST_ANCHOR";
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class ProofRequest
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("self_attested_attributes", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> SelfAttestedAttributes { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("requested_attrs", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, object[]> RequestedAttrs { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("requested_predicates", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> RequestedPredicates { get; set; }
        }


        /// <summary>
        /// TODO
        /// </summary>
        public class SignatureType
        {
            /// <summary>
            /// TODO
            /// </summary>
            public static string CL = "CL";
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class GenesisTransactionPath
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("genesis_txn", NullValueHandling = NullValueHandling.Ignore)]
            public string GenesisTransaction { get; set; }
        }
        /// <summary>
        /// TODO
        /// </summary>
        public class SchemaProofRequest : CredentialSchemaBase
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("nonce", NullValueHandling = NullValueHandling.Ignore)]
            public string Nonce { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("requested_attrs", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, RequestedSchemaAttributes> RequestedAttributes { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("requested_predicates", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, PredicateReferent> RequestedPredicates { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class RequestedSchemaAttributes
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("restrictions", NullValueHandling = NullValueHandling.Ignore)]
            public Restriction[] Restrictions { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class Restriction
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("issuer_did", NullValueHandling = NullValueHandling.Ignore)]
            public string IssuerDid { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("schema_key", NullValueHandling = NullValueHandling.Ignore)]
            public SchemaKey SchemaKey { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class ClaimsForProofResponse
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("attrs", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, Referent[]> Attrs { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("predicates", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, Referent[]> Predicates { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class PredicateReferent
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("attr_name", NullValueHandling = NullValueHandling.Ignore)]
            public string AttrName { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("p_type", NullValueHandling = NullValueHandling.Ignore)]
            public string PType { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
            public long? Value { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("restrictions", NullValueHandling = NullValueHandling.Ignore)]
            public Restriction[] Restrictions { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class ProverClaims
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("referent", NullValueHandling = NullValueHandling.Ignore)]
            public string Referent { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("attrs", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> Attrs { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("revoc_reg_seq_no")]
            public int? RevocRegSequenceNumber { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("schema_key", NullValueHandling = NullValueHandling.Ignore)]
            public SchemaKey SchemaKey { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("issuer_did", NullValueHandling = NullValueHandling.Ignore)]
            public string IssuerDid { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public partial class Referent
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("cred_info", NullValueHandling = NullValueHandling.Ignore)]
            public ReferentDetail CredInfo { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("interval", NullValueHandling = NullValueHandling.Ignore)]
            public string interval { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public partial class ReferentDetail
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("referent", NullValueHandling = NullValueHandling.Ignore)]
            public string ClaimUUID { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("attrs", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> Attrs { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("schema_id", NullValueHandling = NullValueHandling.Ignore)]
            public string SchemaKey { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("rev_reg_id", NullValueHandling = NullValueHandling.Ignore)]
            public string RevRegId { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("cred_rev_id", NullValueHandling = NullValueHandling.Ignore)]
            public string CredRevId { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class ClaimDefinitionResult
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("ref", NullValueHandling = NullValueHandling.Ignore)]
            public int Ref { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("origin", NullValueHandling = NullValueHandling.Ignore)]
            public string Origin { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("signature_type", NullValueHandling = NullValueHandling.Ignore)]
            public string SignatureType { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
            public Data Data { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
            public string Type { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("identifier", NullValueHandling = NullValueHandling.Ignore)]
            public string Identifier { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("seqNo", NullValueHandling = NullValueHandling.Ignore)]
            public long? SeqNo { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("txnTime", NullValueHandling = NullValueHandling.Ignore)]
            public long? TxnTime { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("state_proof", NullValueHandling = NullValueHandling.Ignore)]
            public StateProof StateProof { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("reqId", NullValueHandling = NullValueHandling.Ignore)]
            public long? ReqId { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class OpenPoolConfiguration
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("refresh_on_open", NullValueHandling = NullValueHandling.Ignore)]
            public bool RefreshOnOpen { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("auto_refresh_time", NullValueHandling = NullValueHandling.Ignore)]
            public int AutoRefreshTime { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("network_timeout", NullValueHandling = NullValueHandling.Ignore)]
            public int NetworkTimeout { get; set; }
        }


        /// <summary>
        /// TODO
        /// </summary>
        public class SchemaKey
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
            public string Version { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("did", NullValueHandling = NullValueHandling.Ignore)]
            public string Did { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class KeyCorrectnessProof
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("c", NullValueHandling = NullValueHandling.Ignore)]
            public string C { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("xz_cap", NullValueHandling = NullValueHandling.Ignore)]
            public string XzCap { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("xr_cap", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> ClaimKeyValuePair { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class ClaimOfferResponse
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("issuer_did", NullValueHandling = NullValueHandling.Ignore)]
            public string IssuerDid { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("schema_key", NullValueHandling = NullValueHandling.Ignore)]
            public SchemaKey SchemaKey { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("key_correctness_proof", NullValueHandling = NullValueHandling.Ignore)]
            public KeyCorrectnessProof KeyCorrectnessProof { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("nonce", NullValueHandling = NullValueHandling.Ignore)]
            public string Nonce { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class SchemaResult
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
            public CredentialSchema CredentialSchema { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("dest", NullValueHandling = NullValueHandling.Ignore)]
            public string Destination { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("seqNo", NullValueHandling = NullValueHandling.Ignore)]
            public int? SequenceNumber { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class CredentialSchemaResult
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
            public CredentialSchema CredentialSchema { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("identifier", NullValueHandling = NullValueHandling.Ignore)]
            public string Identifier { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("dest", NullValueHandling = NullValueHandling.Ignore)]
            public string Destination { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("txnTime", NullValueHandling = NullValueHandling.Ignore)]
            public int TransactionTime { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("state_proof", NullValueHandling = NullValueHandling.Ignore)]
            public StateProof StateProof { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
            public string Type { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("reqId", NullValueHandling = NullValueHandling.Ignore)]
            public long RequestId { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("seqNo", NullValueHandling = NullValueHandling.Ignore)]
            public int? SequenceNumber { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class LedgerResult<TResult>
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
            public TResult Result { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("op", NullValueHandling = NullValueHandling.Ignore)]
            public string Op { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class CredentialSchema : CredentialSchemaBase
        {

            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("attr_names", NullValueHandling = NullValueHandling.Ignore)]
            public string[] AttributeNames { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class CredentialSchemaBase
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
            public string Name { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
            public string Version { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class DidEntity
        {
            /// <summary>
            /// TODO
            /// </summary>
            public DidInfo DidInfo { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            public Wallet Wallet { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class DidInfo
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("did", NullValueHandling = NullValueHandling.Ignore)]
            public string Did { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("seed", NullValueHandling = NullValueHandling.Ignore)]
            public string Seed { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("crypto_type", NullValueHandling = NullValueHandling.Ignore)]
            public string CryptoType { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("cid", NullValueHandling = NullValueHandling.Ignore)]
            public bool Cid { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("verkey", NullValueHandling = NullValueHandling.Ignore)]
            public string VerKey { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class ConnectionRequest
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("did", NullValueHandling = NullValueHandling.Ignore)]
            public string Did { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("nonce", NullValueHandling = NullValueHandling.Ignore)]
            public long Nonce { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class ConnectionResponse
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("did", NullValueHandling = NullValueHandling.Ignore)]
            public string Did { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("nonce", NullValueHandling = NullValueHandling.Ignore)]
            public long Nonce { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("verkey", NullValueHandling = NullValueHandling.Ignore)]
            public string VerKey { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class Data
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("primary", NullValueHandling = NullValueHandling.Ignore)]
            public Primary Primary { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("revocation", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> Revocation { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class Primary
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("rctxt", NullValueHandling = NullValueHandling.Ignore)]
            public string Rctxt { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("n", NullValueHandling = NullValueHandling.Ignore)]
            public string N { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("z", NullValueHandling = NullValueHandling.Ignore)]
            public string Z { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("rms", NullValueHandling = NullValueHandling.Ignore)]
            public string Rms { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("s", NullValueHandling = NullValueHandling.Ignore)]
            public string S { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("r", NullValueHandling = NullValueHandling.Ignore)]
            public Dictionary<string, string> R { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class StateProof
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("root_hash", NullValueHandling = NullValueHandling.Ignore)]
            public string RootHash { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("multi_signature", NullValueHandling = NullValueHandling.Ignore)]
            public MultiSignature MultiSignature { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("proof_nodes", NullValueHandling = NullValueHandling.Ignore)]
            public string ProofNodes { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class MultiSignature
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("signature", NullValueHandling = NullValueHandling.Ignore)]
            public string Signature { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
            public Value Value { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("participants", NullValueHandling = NullValueHandling.Ignore)]
            public string[] Participants { get; set; }
        }

        /// <summary>
        /// TODO
        /// </summary>
        public class Value
        {
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("state_root_hash", NullValueHandling = NullValueHandling.Ignore)]
            public string StateRootHash { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("txn_root_hash", NullValueHandling = NullValueHandling.Ignore)]
            public string TxnRootHash { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
            public long? Timestamp { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("ledger_id", NullValueHandling = NullValueHandling.Ignore)]
            public long? LedgerId { get; set; }
            /// <summary>
            /// TODO
            /// </summary>
            [JsonProperty("pool_state_root_hash", NullValueHandling = NullValueHandling.Ignore)]
            public string PoolStateRootHash { get; set; }
        }
    }
}
