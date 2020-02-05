using System;
using CanaryCollector.Collectors;

namespace CanaryCollector.Remote.Jira
{
    public class JiraTicketProviderFactory : ITicketProviderFactory
    {
        private readonly Uri jiraUri;
        private readonly JiraCredentials credentials;

        public JiraTicketProviderFactory(Uri jiraUri)
        {
            if (jiraUri == null) throw new ArgumentNullException(nameof(jiraUri));
            var builder = new UriBuilder(jiraUri);
            if (!String.IsNullOrWhiteSpace(builder.UserName))
            {
                credentials = new JiraCredentials(builder.UserName, builder.Password);
            }
            builder.UserName = "";
            builder.Password = "";
            this.jiraUri = builder.Uri;
        }

        public ITicketProvider Create(string dependentParameter)
        {
            var jiraConnection = Atlassian.Jira.Jira.CreateRestClient(jiraUri.ToString(), credentials.UserName, credentials.Password);
            return new JiraTicketProvider(jiraConnection);
        }
    }
}
