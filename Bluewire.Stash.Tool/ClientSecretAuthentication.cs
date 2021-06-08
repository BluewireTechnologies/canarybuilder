using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Bluewire.Stash.Tool
{
    public class ClientSecretAuthentication : IAuthentication
    {
        private readonly string secret;

        public ClientSecretAuthentication(string secret)
        {
            this.secret = secret;
        }

        public async Task<IAuthenticationProvider> Create()
        {
            return await ClientSecretAuthenticationProvider.Create(secret);
        }

        public async Task<AuthenticationResult?> Test(TextWriter stdout, CancellationToken token)
        {
            var auth = await ClientSecretAuthenticationProvider.Create(secret);

            // With PublicClientAuthenticationProvider, the user can authenticate interactively by
            // using the 'authenticate' command, and then retry 'diagnostics' to potentially get
            // a successful result.
            // No such possibility exists with client secret auth, so simply rethrow on failure.
            var authResult = await auth.Authenticate(token);
            return authResult;
        }
    }
}
