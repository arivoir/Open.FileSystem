using System;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public interface IAuthenticationBroker
    {
        Task<Uri> WebAuthenticationBrokerAsync(Uri url,
            Uri callbackUrl,
            bool isScriptEnabled = true);

        Task<AuthenticatonTicket> FormAuthenticationBrokerAsync(Func<string, string, string, string, bool, Task<AuthenticatonTicket>> authenticationCallback,
            string providerName,
            string backgroundColor,
            string iconResourceKey,
            string server = null,
            string domain = null,
            string user = null,
            string password = null,
            bool showServer = true,
            bool showDomain = true,
            bool userNameIsEmail = false,
            bool userAndPasswordRequired = true);
    }
}