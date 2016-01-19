using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CanaryCollector.Collectors;
using CanaryCollector.Model;
using YouTrackSharp.Infrastructure;
using YouTrackSharp.Issues;

namespace CanaryCollector.Remote.YouTrack
{
    public class YouTrackTicketProvider : ITicketProvider
    {
        private readonly Connection youtrackConnection;

        public YouTrackTicketProvider(Connection youtrackConnection)
        {
            this.youtrackConnection = youtrackConnection;
        }

        public IEnumerable<IssueTicket> GetTicketsPendingReview()
        {
            var issueManagement = new IssueManagement(youtrackConnection);

            return issueManagement.GetIssuesBySearch("State: {Pending Review}").Select(ReadTicket).ToArray();

        }

        public IEnumerable<IssueTicket> GetTicketsWithTag(string tagName)
        {
            var issueManagement = new IssueManagement(youtrackConnection);

            return issueManagement.GetIssuesBySearch($"tag: {{{tagName}}}").Select(ReadTicket).ToArray();
        }

        private static IssueTicket ReadTicket(Issue issue)
        {
            object type;
            issue.TryGetMember(new TicketTypeProperty(), out type);

            return new IssueTicket { Identifier = issue.Id, Type = TicketTypeProperty.Convert(type) };
        }

        class TicketTypeProperty : GetMemberBinder
        {
            public TicketTypeProperty() : base("Type", true)
            {
            }

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                return errorSuggestion;
            }

            private static readonly StringComparer comparer = StringComparer.InvariantCultureIgnoreCase;
            public static IssueTicketType Convert(object type)
            {
                if (comparer.Equals(type, "bug")) return IssueTicketType.Bug;
                if (comparer.Equals(type, "usability problem")) return IssueTicketType.UsabilityProblem;
                if (comparer.Equals(type, "feature")) return IssueTicketType.Feature;
                if (comparer.Equals(type, "technical debt")) return IssueTicketType.TechnicalDebt;
                if (comparer.Equals(type, "performance")) return IssueTicketType.Performance;
                return IssueTicketType.Unknown;
            }
        }
    }
}