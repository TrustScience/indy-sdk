using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAgent.Models
{

    public class AccountCreationRequestDto
    {
        public string Email { get; set; }
    }

    public class BindingRequestDto
    {
        public string Email { get; set; }
    }

    public class ConnectionRequestDto
    {
        public string AppToUserDid { get; set; }
        public string Nonce { get; set; }
    }

    public class ConnectionResponseDto
    {
        public string Email { get; set; }
        public byte[] EncryptedConnectionResponsePayload { get; set; }
    }

    public class ConnectionResponsePayloadDto
    {
        public string UserToAppDid { get; set; }
        public string UserToAppVerKey { get; set; }
        public string Nonce { get; set; }
    }
}
