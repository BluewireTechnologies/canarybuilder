using System;
using System.Linq;
using System.Net;

namespace CanaryCollector.Remote.YouTrack
{
    public struct YouTrackConnectionSettings
    {
        public static YouTrackConnectionSettings Parse(Uri uri)
        {
            var userInfoParts = uri.UserInfo.Split(':');

            return new YouTrackConnectionSettings
            {
                Host = uri.Host,
                Port = uri.Port,
                Path = uri.AbsolutePath.Trim('/'),
                UseSSL = uri.Scheme == Uri.UriSchemeHttps,
                Credentials = String.IsNullOrWhiteSpace(uri.UserInfo) ? null : new NetworkCredential(userInfoParts.First(), userInfoParts.ElementAtOrDefault(1))
            };
        }

        public string Host { get; set; }
        public int Port { get; set; }
        public string Path { get; set; }
        public NetworkCredential Credentials { get; set; }
        public bool UseSSL { get; set; }
    }
}

