using System.Collections.Generic;

namespace WebAgent.Models
{
    public static class Repository 
    {
        public static string WebAppDid;
        public static List<Account> Accounts = new List<Account>();
    }

    public class Account
    {
        public string Email { get; set; }
        public string AppToUserDid { get; set; }
        public string UserToAppDid { get; set; }
        public string Nonce { get; set; }
    }
    
}
