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
    public class AuthenticationProvider
    {
        private static PublicClientApplicationOptions GetOptions()
        {
            return new PublicClientApplicationOptions
            {
                Instance = ConfigurationManager.AppSettings["AzureAd.Instance"],
                TenantId = ConfigurationManager.AppSettings["AzureAd.TenantId"],
                ClientId = ConfigurationManager.AppSettings["AzureAd.ClientId"],
            };
        }

        public static async Task<AuthenticationProvider> Create()
        {
            var appConfiguration = GetOptions();

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

            return new AuthenticationProvider(app);
        }

        private readonly IPublicClientApplication app;

        private AuthenticationProvider(IPublicClientApplication app)
        {
            this.app = app;
        }

        public async Task<string[]> ListCachedAccounts()
        {
            var accounts = await app.GetAccountsAsync();
            return accounts.Select(a => a.Username).ToArray();
        }

        private readonly string[] scopes = { "a03f7280-a152-4b55-af50-95cda50b0009/access_as_user" };

        public async Task<AuthenticationResult> AuthenticateCached(CancellationToken token)
        {
            var accounts = await app.GetAccountsAsync();

            // Try to acquire an access token from the cache. If an interaction is required,
            // MsalUiRequiredException will be thrown.
            return await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
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
                return await app.AcquireTokenInteractive(scopes)
                    .WithUseEmbeddedWebView(true)
                    .ExecuteAsync(token);
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
