using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Bluewire.Stash.Tool
{
    public class ClientSecretAuthenticationProvider : IAuthenticationProvider
    {
        public static async Task<ClientSecretAuthenticationProvider> Create(string secret)
        {
            var appConfiguration = new ConfidentialClientApplicationOptions
            {
                Instance = AuthenticationSettings.Instance,
                TenantId = AuthenticationSettings.TenantId,
                ClientId = AuthenticationSettings.ClientId,
                ClientSecret = secret,
            };

            // Building the AAD authority, https://login.microsoftonline.com/<tenant>
            var authority = new Uri(new Uri(appConfiguration.Instance), appConfiguration.TenantId);

            var app = ConfidentialClientApplicationBuilder.Create(appConfiguration.ClientId)
                .WithClientSecret(appConfiguration.ClientSecret)
                .WithAuthority(authority)
                .Build();

            return new ClientSecretAuthenticationProvider(app);
        }

        private readonly IConfidentialClientApplication app;

        private ClientSecretAuthenticationProvider(IConfidentialClientApplication app)
        {
            this.app = app;
        }

        public async Task<AuthenticationResult> Authenticate(CancellationToken token)
        {
            return await app.AcquireTokenForClient(AuthenticationSettings.ConfidentialScopes)
                .ExecuteAsync(token);
        }

        public Task Clear() => Task.CompletedTask;
    }
}
