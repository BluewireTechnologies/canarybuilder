using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Bluewire.Stash.Tool
{
    public class PublicClientAuthentication : IAuthentication
    {
        public async Task<IAuthenticationProvider> Create()
        {
            return await PublicClientAuthenticationProvider.Create();
        }

        public async Task<AuthenticationResult?> Test(TextWriter stdout, CancellationToken token)
        {
            var auth = await PublicClientAuthenticationProvider.Create();
            var accounts = await auth.ListCachedAccounts();
            if (!accounts.Any())
            {
                stdout.WriteLine("No cached accounts.");
                return null;
            }

            stdout.WriteLine("Cached accounts:");
            foreach (var account in accounts)
            {
                stdout.WriteLine($" * {account}");
            }

            var authResult = await TryAuthenticate(stdout, auth, token);
            if (authResult == null)
            {
                stdout.WriteLine("Cached credentials are no longer valid. Please use the 'authenticate' command to renew them.");
                return null;
            }
            return authResult;
        }

        private async Task<AuthenticationResult?> TryAuthenticate(TextWriter stdout, PublicClientAuthenticationProvider auth, CancellationToken token)
        {
            try
            {
                return await auth.AuthenticateCached(token);
            }
            catch (MsalUiRequiredException)
            {
                return null;
            }
        }
    }
}
