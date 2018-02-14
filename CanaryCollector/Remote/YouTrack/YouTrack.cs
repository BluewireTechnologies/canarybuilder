using System;
using YouTrackSharp.Infrastructure;

namespace CanaryCollector.Remote.YouTrack
{
    class YouTrack
    {
        public static Connection OpenConnection(Uri uri)
        {
            var settings = YouTrackConnectionSettings.Parse(uri);
            var connection = new Connection(settings.Host, settings.Port, settings.UseSSL, settings.Path);
            if (settings.Credentials != null)
            {
                connection.Authenticate(settings.Credentials);
            }
            return connection;
        }
    }
}

