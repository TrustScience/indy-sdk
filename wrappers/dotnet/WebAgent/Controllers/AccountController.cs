using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hyperledger.Indy.CryptoApi;
using Hyperledger.Indy.DidApi;
using Hyperledger.Indy.LedgerApi;
using Hyperledger.Indy.WalletApi;
using indy_sdk_spike;
using Microsoft.AspNetCore.Mvc;
using WebAgent.Models;

namespace WebAgent.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        [HttpPost]
        public object Post([FromBody]AccountCreationRequestDto request)
        {
            if(Repository.Accounts.Any(x => x.Email == request.Email))
            {
                return BadRequest("Email already used");
            }

            var account = new Account()
            {
                Email = request.Email
            };
            Repository.Accounts.Add(account);
            return Ok(account);
        }

        [HttpGet]
        public Account Get([FromBody]string email)
        {
            return Repository.Accounts.FirstOrDefault(a => a.Email == email);
        }

        [HttpPost]
        [Route("getConnectionRequest")]
        public async Task<ConnectionRequestDto> GetRequest([FromBody]BindingRequestDto request)
        {

            var appToUserDidResult = Did.CreateAndStoreMyDidAsync(Config.WebAppWallet, "{}").Result;
            var nymRequest = await Ledger.BuildNymRequestAsync(Config.WebAppDid, appToUserDidResult.Did, appToUserDidResult.VerKey, null, null);
            await Ledger.SignAndSubmitRequestAsync(Config.WebAppPool, Config.WebAppWallet, Config.WebAppDid, nymRequest);


            var account = Repository.Accounts.First(x => x.Email == request.Email);
            account.AppToUserDid = appToUserDidResult.Did;
            account.Nonce = Guid.NewGuid().ToString();

            var connectionRequest = new ConnectionRequestDto();
            connectionRequest.AppToUserDid = appToUserDidResult.Did;
            connectionRequest.Nonce = account.Nonce;

            return connectionRequest;
        }

        [Route("sendConnectionResponse")]
        [HttpPost]
        public async Task<object> Post([FromBody]ConnectionResponseDto response)
        {
            var account = Repository.Accounts.First(x => x.Email == response.Email);
            var appToUserVerKey = await Did.KeyForDidAsync(Config.WebAppPool, Config.WebAppWallet, account.AppToUserDid);
            var decryptedConnectionResponse = (await Crypto.AnonDecryptAsync(Config.WebAppWallet, appToUserVerKey, response.EncryptedConnectionResponsePayload))
                .Serialize<ConnectionResponsePayloadDto>();

            if(decryptedConnectionResponse.Nonce != account.Nonce)
            {
                return BadRequest("Nonce not match");
            }

            account.UserToAppDid = decryptedConnectionResponse.UserToAppDid;

            var nymRequest = await Ledger.BuildNymRequestAsync(Config.WebAppDid, decryptedConnectionResponse.UserToAppDid, decryptedConnectionResponse.UserToAppVerKey, null, null);
            await Ledger.SignAndSubmitRequestAsync(Config.WebAppPool, Config.WebAppWallet, Config.WebAppDid, nymRequest);

            return Ok();
        }

    }


}
