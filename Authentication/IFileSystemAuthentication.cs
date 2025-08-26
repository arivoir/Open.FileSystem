using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystemAsync
{
    public interface IFileSystemAuthentication
    {
        Task<AuthenticatonTicket> RefreshTokenAsync(string connectionString, CancellationToken cancellationToken);
        Task<AuthenticatonTicket> LogInAsync(IAuthenticationBroker authenticationBroker, string connectionString, string[] scopes, bool requestingDeniedScope, CancellationToken cancellationToken);
    }
}