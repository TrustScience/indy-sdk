using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.PoolApi;
using Hyperledger.Indy.WalletApi;
using indy_sdk_spike;
using RestSharp;
using System;
using System.Threading.Tasks;
using WebAgent;
using WebAgent.Models;
using static Hyperledger.Indy.IndySDKModels;

namespace ConsoleEdgeAgent
{
    class Program
    {
        const string BaseUrl = "http://localhost:50109/api";
        const string Seed = "000000000000000000000000Console2";
        const string WalletName = "consolewallet";
        const string Email = "coolguy@gmail.com";

        static Wallet _wallet;
        static string _did;
        static string _verKey;
        static string _poolName = WebAgent.Config.WebAppPoolName;
        static Pool _pool;

        static void Main(string[] args)
        {
            // initialize
            _pool = IndyUtils.CreatePool(_poolName).Result;
            (_wallet, _did, _verKey) = IndyUtils.CreateWalletIfNotExist(Seed, _poolName, WalletName).Result;

            // create account
            CreateAccount(Email);

            // get connection request to bind account
            var request = GetConnectionRequest(Email);

            // send connection response
            var response = CreateConnectionResponse(request).Result;
            SendConnectionResponse(response);

        }

        private static async Task<ConnectionResponseDto> CreateConnectionResponse(ConnectionRequestDto connectionRequest)
        {
            var fromToDid = await Did.CreateAndStoreMyDidAsync(_wallet, "{}");

            var connectionResponsePayload = new ConnectionResponsePayloadDto()
            {
                UserToAppDid = fromToDid.Did,
                UserToAppVerKey = fromToDid.VerKey,
                Nonce = connectionRequest.Nonce
            };

            var toFromVerKey = await Did.KeyForDidAsync(_pool, _wallet, connectionRequest.AppToUserDid);
            var anonEncryptedConnectionResponse = await Crypto.AnonCryptAsync(toFromVerKey, connectionResponsePayload.ToJsonThenBytes());

            var connectionResponseDto = new ConnectionResponseDto()
            {
                Email = Email,
                EncryptedConnectionResponsePayload = anonEncryptedConnectionResponse
            };

            return connectionResponseDto;
        }

        private static string CreateAccount(string email)
        {
            var resource = "account/";
            var body = new { Email = email };
            return Post(resource, body);
        }

        private static ConnectionRequestDto GetConnectionRequest(string email)
        {
            var resource = "account/getConnectionRequest/";
            var body = new { Email = email };
            var connectionRequestString =  Post(resource, body);
            return connectionRequestString.Serialize<ConnectionRequestDto>();
        }

        private static void SendConnectionResponse(ConnectionResponseDto response)
        {
            var resource = "account/sendConnectionResponse/";
            var body = new { Email = response.Email, EncryptedConnectionResponsePayload = response.EncryptedConnectionResponsePayload };
            var result = Post(resource, body);
        }

        private static string Post(string resource, object body, string contentType = Constants.JsonContentType)
        {
            var client = new RestClient(BaseUrl);
            var request = new RestRequest(resource, Method.POST);
            request.AddHeader("Content-Type", contentType);
            if (contentType == Constants.JsonContentType)
            {
                request.RequestFormat = DataFormat.Json;
            }
            request.AddBody(body);
            var response = client.Execute(request);
            var content = response.Content;
            return content;
        }
    }


    public static class Constants
    {
        public const string ByteContentType = "application/octet-stream";
        public const string JsonContentType = "application/json";

    }
}
