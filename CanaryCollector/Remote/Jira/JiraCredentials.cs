namespace CanaryCollector.Remote.Jira
{
    class JiraCredentials
    {
        public string UserName { get; }
        public string Password { get; }

        public JiraCredentials(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }
    }
}
