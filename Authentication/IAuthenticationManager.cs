using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystem
{
    public interface IAuthenticationManager
    {
        IFileSystemAuthentication FileSystem { get; set; }
        string ConnectionString { get; set; }
        event EventHandler<AsyncEventArgs> ConnectionStringChanged;

        Task<AuthenticatonTicket> AddNewAsync(string[] scopes, CancellationToken cancellationToken);
        Task<AuthenticatonTicket> AuthenticateAsync(string[] scopes, bool promptForUserInteraction, CancellationToken cancellationToken);

        void InvalidateCredentials();
    }
}