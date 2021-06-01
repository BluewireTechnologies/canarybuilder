using System.Configuration;

namespace Bluewire.Stash.Tool
{
    class AuthenticationSettings
    {
        public static string Instance => ConfigurationManager.AppSettings["AzureAd.Instance"];
        public static string TenantId => ConfigurationManager.AppSettings["AzureAd.TenantId"];
        public static string ClientId => ConfigurationManager.AppSettings["AzureAd.ClientId"];

        public static string[] PublicScopes => new [] { "a03f7280-a152-4b55-af50-95cda50b0009/access_as_user" };
        public static string[] ConfidentialScopes => new [] { "a03f7280-a152-4b55-af50-95cda50b0009/.default" };
    }
}
