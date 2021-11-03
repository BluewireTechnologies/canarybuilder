using System;
using Atlassian.Jira;
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
            var settings = new JiraRestClientSettings();

            // This is required due to GDPR changes specific to the Cloud version https://bitbucket.org/farmas/atlassian.net-sdk/issues/509/jirauser-api-v2-updated-for-gdpr-removing.
            // without this, the package can't understand custom user fields that Jira Cloud refuses to return.
            // This means we will be returned accountIds rather than any user information, but we don't need that information here.
            settings.EnableUserPrivacyMode = true;

            var jiraConnection = Atlassian.Jira.Jira.CreateRestClient(jiraUri.ToString(), credentials.UserName, credentials.Password);
            return new JiraTicketProvider(jiraConnection);
        }
    }
}
