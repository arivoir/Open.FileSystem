using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Open.FileSystemAsync
{
    public class AuthenticationManager : IAuthenticationManager
    {
        public AuthenticationManager(IAuthenticationBroker authenticationBroker)
        {
            AuthenticationBroker = authenticationBroker;
        }

        public string ConnectionString { get; set; }

        public event EventHandler<AsyncEventArgs> ConnectionStringChanged;

        private AuthenticatonTicket AuthenticatonTicket { get; set; }
        public IFileSystemAuthentication FileSystem { get; set; }
        public IAuthenticationBroker AuthenticationBroker { get; private set; }


        public virtual async Task<AuthenticatonTicket> AddNewAsync(string[] scopes, CancellationToken cancellationToken)
        {
            var ticket = await FileSystem.LogInAsync(AuthenticationBroker, ConnectionString, scopes, false, cancellationToken);
            AuthenticatonTicket = ticket;
            await SaveConnectionString(ticket.RefreshToken ?? ticket.AuthToken);
            return ticket;
        }

        public virtual async Task<AuthenticatonTicket> AuthenticateAsync(string[] scopes, bool promptForUserInteraction, CancellationToken cancellationToken)
        {
            bool requestingDeniedScope = false;
            bool requestingNewScope = false;
            if (AuthenticatonTicket != null)
            {
                ProcessTicket(AuthenticatonTicket, scopes, out requestingNewScope, out requestingDeniedScope);
                if (!requestingNewScope &&
                    !requestingDeniedScope &&
                    (!AuthenticatonTicket.ExpirationTime.HasValue || AuthenticatonTicket.ExpirationTime.Value > DateTime.Now))
                {
                    return AuthenticatonTicket;
                }
            }

            if (!string.IsNullOrWhiteSpace(ConnectionString) &&
                !requestingNewScope &&
                !requestingDeniedScope)
            {
                try
                {

                    var ticket = await FileSystem.RefreshTokenAsync(ConnectionString, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(ticket.RefreshToken) && ticket.RefreshToken != ConnectionString)
                    {
                        await SaveConnectionString(ticket.RefreshToken);
                    }
                    ProcessTicket(ticket, scopes, out requestingNewScope, out requestingDeniedScope);
                    if (!string.IsNullOrWhiteSpace(ticket.AuthToken))
                    {
                        AuthenticatonTicket = ticket;
                        if (!requestingNewScope && !requestingDeniedScope)
                            return ticket;
                    }
                }
                catch (AccessDeniedException) { }
            }
            if (promptForUserInteraction)
            {
                var ticket = await FileSystem.LogInAsync(AuthenticationBroker, ConnectionString, scopes, requestingDeniedScope, cancellationToken);
                ProcessTicket(ticket, scopes, out requestingNewScope, out requestingDeniedScope);
                if (requestingNewScope || requestingDeniedScope)
                    throw new AccessDeniedException();
                if (ticket.RefreshToken != null)
                {
                    await SaveConnectionString(ticket.RefreshToken);
                    if (ticket.AuthToken == null)
                    {
                        ticket = await FileSystem.RefreshTokenAsync(ticket.RefreshToken, cancellationToken);
                    }
                }
                else
                {
                    await SaveConnectionString(ticket.AuthToken);
                }
                AuthenticatonTicket = ticket;
                return ticket;
            }
            else
            {
                throw new AccessDeniedException();
            }
        }

        private static void ProcessTicket(AuthenticatonTicket ticket, string[] scopes, out bool requestNewScopes, out bool requestDeniedScopes)
        {
            scopes = scopes != null ? scopes : new string[0];
            var grantedScopes = ticket.GrantedScopes != null ? ticket.GrantedScopes : new string[0];
            var deniedScopes = ticket.DeclinedScopes != null ? ticket.DeclinedScopes : new string[0];
            if (scopes.All(scope => grantedScopes.Contains(scope)))
            {
                requestNewScopes = false;
                requestDeniedScopes = false;
            }
            else
            {
                requestNewScopes = true;
            }
            if (scopes.Any(scope => deniedScopes.Contains(scope)))
            {
                requestDeniedScopes = true;
            }
            else
            {
                requestDeniedScopes = false;
            }
        }

        public virtual void InvalidateCredentials()
        {
            AuthenticatonTicket = null;
        }

        protected Task SaveConnectionString(string connectionString)
        {
            ConnectionString = connectionString;
            var e = new AsyncEventArgs();
            ConnectionStringChanged?.Invoke(this, e);
            return e.WaitDeferralsAsync();
        }
    }
}
