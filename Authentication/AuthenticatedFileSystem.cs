using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystemAsync
{
    public abstract class AuthenticatedFileSystem : FileSystemAsync, IFileSystemAuthentication
    {
        #region fields

        private IAuthenticationManager _authenticationManager;
        private SemaphoreSlim _getAccessTokenSemaphore = new SemaphoreSlim(1);

        #endregion

        #region object model

        public IAuthenticationManager AuthenticationManager
        {
            get
            {
                return _authenticationManager;
            }
            set
            {
                _authenticationManager = value;
                _authenticationManager.FileSystem = this;
            }
        }

        #endregion

        #region implementation

        protected Task<string> GetAccessTokenAsync(bool promptForUserInteraction, CancellationToken cancellationToken)
        {
            return GetAccessTokenAsync(GetScopes(""), promptForUserInteraction, cancellationToken);
        }

        protected Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            return GetAccessTokenAsync(null, true, cancellationToken);
        }

        protected async Task<string> GetAccessTokenAsync(IEnumerable<string> scopes = null, bool promptForUserInteraction = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            var ticket = await AuthenticateAsync(scopes, promptForUserInteraction, cancellationToken);
            return ticket?.AuthToken;
        }

        protected virtual async Task<AuthenticatonTicket> AuthenticateAsync(IEnumerable<string> scopes = null, bool promptForUserInteraction = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (AuthenticationManager == null)
                return null;

            try
            {
                await _getAccessTokenSemaphore.WaitAsync();
                scopes = scopes ?? new string[0];
                var ticket = await AuthenticationManager.AuthenticateAsync(scopes.ToArray(), promptForUserInteraction, cancellationToken);
                return ticket;
            }
            finally { _getAccessTokenSemaphore.Release(); }
        }

        protected async override Task<bool> CheckAccessAsyncOverride(string dirId, bool promptForUserInteraction, CancellationToken cancellationToken)
        {
            var scopes = GetScopes(dirId);
            var ticket = await GetAccessTokenAsync(scopes, promptForUserInteraction, cancellationToken);
            return ticket != null;
        }

        public virtual string[] GetScopes(string dirId)
        {
            return new string[0];
        }

        protected override Task InvalidateAccessAsyncOverride(string dirId, CancellationToken cancellationToken)
        {
            AuthenticationManager?.InvalidateCredentials();
            return Task.FromResult(true);
        }

        public abstract Task<AuthenticatonTicket> RefreshTokenAsync(string connectionString, CancellationToken cancellationToken);

        public abstract Task<AuthenticatonTicket> LogInAsync(IAuthenticationBroker authenticationBroker, string connectionString, string[] scopes, bool requestingDeniedScope, CancellationToken cancellationToken);

        #endregion
    }
}
