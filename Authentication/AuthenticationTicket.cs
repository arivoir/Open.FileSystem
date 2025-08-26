using System;

namespace Open.FileSystem
{
    public class AuthenticatonTicket
    {
        public string UserId { get; set; }
        public string AuthToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public string[] GrantedScopes { get; set; }
        public string[] DeclinedScopes { get; set; }
        public object Tag { get; set; }
    }
}
