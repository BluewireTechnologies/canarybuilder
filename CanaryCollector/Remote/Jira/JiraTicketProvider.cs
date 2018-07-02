using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Atlassian.Jira;
using CanaryCollector.Collectors;
using CanaryCollector.Model;

namespace CanaryCollector.Remote.Jira
{
    public class JiraTicketProvider : ITicketProvider
    {
        private readonly Atlassian.Jira.Jira jira;

        public JiraTicketProvider(Atlassian.Jira.Jira jira)
        {
            if (jira == null) throw new ArgumentNullException(nameof(jira));
            this.jira = jira;
        }

        public async Task<IssueTicket[]> GetTicketsPendingMerge()
        {
            var linearWorkflowIssues = GetLinearWorkflowIssues().Take(500).ToArray();
            var pullRequests = GetIncompletePullRequests().Take(500).ToArray();
            var pullRequestParents = await jira.Issues.GetIssuesAsync(pullRequests.Select(s => s.ParentIssueKey).Distinct());

            return linearWorkflowIssues.Concat(pullRequestParents.Values).Select(ReadTicket).ToArray();
        }

        private IQueryable<Issue> GetLinearWorkflowIssues() => jira.Issues.Queryable.Where(t => t.Status == "pending review" || t.Status == "test");
        private IQueryable<Issue> GetIncompletePullRequests() => jira.Issues.Queryable.Where(s => s.Type == "pull request").Where(s => s.Resolution == null);

        public async Task<IssueTicket[]> GetTicketsWithTag(string tagName)
        {
            var issues = await jira.Issues.GetIssuesFromJqlAsync($"Labels = '{tagName}' and Resolution is null", 500);
            return issues.Select(ReadTicket).ToArray();
        }

        private static readonly StringComparer comparer = StringComparer.InvariantCultureIgnoreCase;
        private static IssueTicketType ConvertType(IssueType type)
        {
            if (comparer.Equals(type.Name, "bug")) return IssueTicketType.Bug;
            if (comparer.Equals(type.Name, "usability problem")) return IssueTicketType.UsabilityProblem;
            if (comparer.Equals(type.Name, "feature")) return IssueTicketType.Feature;
            if (comparer.Equals(type.Name, "technical debt")) return IssueTicketType.TechnicalDebt;
            if (comparer.Equals(type.Name, "performance")) return IssueTicketType.Performance;
            return IssueTicketType.Unknown;
        }

        private static IssueTicket ReadTicket(Issue issue)
        {
            return new IssueTicket { Identifier = issue.Key.ToString(), Type = ConvertType(issue.Type) };
        }
    }
}
