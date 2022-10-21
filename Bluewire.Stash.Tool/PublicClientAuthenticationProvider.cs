using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace Bluewire.Stash.Tool
{
    public class PublicClientAuthenticationProvider : IAuthenticationProvider
    {
        public static async Task<PublicClientAuthenticationProvider> Create()
        {
            var appConfiguration = new PublicClientApplicationOptions
            {
                Instance = AuthenticationSettings.Instance,
                TenantId = AuthenticationSettings.TenantId,
                ClientId = AuthenticationSettings.ClientId,
            };

            // Building the AAD authority, https://login.microsoftonline.com/<tenant>
            var authority = new Uri(new Uri(appConfiguration.Instance), appConfiguration.TenantId);

            // Building a public client application
            var app = PublicClientApplicationBuilder.Create(appConfiguration.ClientId)
                .WithAuthority(authority)
                .WithDefaultRedirectUri()
                .Build();

            // Building StorageCreationProperties
            var storageProperties =
                new StorageCreationPropertiesBuilder(CacheSettings.CacheFileName, CacheSettings.CacheDir, appConfiguration.ClientId)
                    .WithLinuxKeyring(
                        CacheSettings.LinuxKeyRingSchema,
                        CacheSettings.LinuxKeyRingCollection,
                        CacheSettings.LinuxKeyRingLabel,
                        CacheSettings.LinuxKeyRingAttr1,
                        CacheSettings.LinuxKeyRingAttr2)
                    .WithMacKeyChain(
                        CacheSettings.KeyChainServiceName,
                        CacheSettings.KeyChainAccountName)
                    .Build();

            // This hooks up the cross-platform cache into MSAL
            var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
            cacheHelper.RegisterCache(app.UserTokenCache);

            return new PublicClientAuthenticationProvider(app);
        }

        private readonly IPublicClientApplication app;

        private PublicClientAuthenticationProvider(IPublicClientApplication app)
        {
            this.app = app;
        }

        public async Task<string[]> ListCachedAccounts()
        {
            var accounts = await app.GetAccountsAsync();
            return accounts.Select(a => a.Username).ToArray();
        }

        public async Task<AuthenticationResult> AuthenticateCached(CancellationToken token)
        {
            var accounts = await app.GetAccountsAsync();

            // Try to acquire an access token from the cache. If an interaction is required,
            // MsalUiRequiredException will be thrown.
            return await app.AcquireTokenSilent(AuthenticationSettings.PublicScopes, accounts.FirstOrDefault())
                .ExecuteAsync(token);
        }

        public async Task<AuthenticationResult> Authenticate(CancellationToken token)
        {
            AuthenticationResult result;
            try
            {
                // Try to acquire an access token from the cache. If an interaction is required,
                // MsalUiRequiredException will be thrown.
                return await AuthenticateCached(token);
            }
            catch (MsalUiRequiredException)
            {
                // Acquiring an access token interactively. MSAL will cache it so we can use AcquireTokenSilent
                // on future calls.
                return await app.AcquireTokenInteractive(AuthenticationSettings.PublicScopes)
                    .WithUseEmbeddedWebView(true)
                    .ExecuteAsync(token);
            }
        }

        public async Task Clear()
        {
            var accounts = await app.GetAccountsAsync();
            foreach (var account in accounts)
            {
                await app.RemoveAsync(account);
            }
        }

        public static class CacheSettings
        {
            private static readonly string cacheFilePath =
                Path.Combine(MsalCacheHelper.UserRootDirectory, "msal.epro.com.cache");

            public static readonly string CacheFileName = Path.GetFileName(cacheFilePath);
            public static readonly string CacheDir = Path.GetDirectoryName(cacheFilePath);


            public static readonly string KeyChainServiceName = "Bluewire.Stash.Tool";
            public static readonly string KeyChainAccountName = "MSALCache";

            public static readonly string LinuxKeyRingSchema = "com.epro.stash.tool.tokencache";
            public static readonly string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
            public static readonly string LinuxKeyRingLabel = "MSAL token cache for Bluewire.Stash.Tool.";
            public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new KeyValuePair<string, string>("Version", "1");
            public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new KeyValuePair<string, string>("ProductGroup", "MyApps");
        }
    }
}
