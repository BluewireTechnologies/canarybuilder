using System;
using Bluewire.Common.Console;
using CanaryCollector.Collectors;

namespace CanaryCollector.Remote.YouTrack
{
    public class YouTrackTicketProviderFactory : ITicketProviderFactory
    {
        private readonly Uri youtrackUri;

        public YouTrackTicketProviderFactory(Uri youtrackUri)
        {
            if (youtrackUri == null) throw new ArgumentNullException(nameof(youtrackUri));
            this.youtrackUri = youtrackUri;
        }

        public ITicketProvider Create(string dependentParameter)
        {
            try
            {
                var youtrackConnection = YouTrack.OpenConnection(youtrackUri);
                return new YouTrackTicketProvider(youtrackConnection);
            }
            catch (Exception ex)
            {
                throw new InvalidArgumentsException(ex);
            }
        }
    }
}
