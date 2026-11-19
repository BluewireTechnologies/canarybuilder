using Atlassian.Jira;
using McMaster.Extensions.CommandLineUtils;

namespace JiraTime
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var app = new CommandLineApplication();
            app.HelpOption(true);

            var userOption = app.Option<string>("-u|--user <email>",
                "User name to request time tracking information for.",
                CommandOptionType.SingleValue,
                o => o.Inherited = true);

            var dayOption = app.Option<string>("-d|--day <date>",
                "Date",
                CommandOptionType.SingleValue,
                o => o.Inherited = true);

            app.OnExecuteAsync(async token => {
                var date = GetDate(dayOption.ParsedValue);

                const string jiraCloudId = "59db4a5c-1674-44a1-905a-0daea35ee4bb";
                var url = new Uri($"https://api.atlassian.com/ex/jira/{jiraCloudId}");

                var key = Prompt.GetPassword("Enter API key:");

                var jiraClient = Jira.CreateRestClient(url.OriginalString, userOption.ParsedValue, key, new JiraRestClientSettings { EnableUserPrivacyMode = true });

                var start = date.Date;
                var end = date.AddDays(1).Date;
                var worklogs = await GetWorklogsBetween(jiraClient, userOption.ParsedValue, start, end, token).ToListAsync(token);

                var totalTime = worklogs.Aggregate(TimeSpan.Zero, (a, x) => a + x.Duration);
                var clamped = worklogs.Select(ClampToFifteenMinuteIntervals).OrderBy(x => x.Start).ToArray();

                app.Out.WriteLine($"User: {userOption.ParsedValue}");
                app.Out.WriteLine($"Date: {start.Date:dd MMM yyyy}");
                app.Out.WriteLine($"Time logged: {totalTime:hh\\:mm}");
                app.Out.WriteLine();

                if (clamped.Length > 0)
                {
                    var lastEnd = clamped.First().Start;
                    foreach (var entry in clamped)
                    {
                        var gap = entry.Start - lastEnd;
                        if (gap.TotalMinutes >= 1)
                        {
                            app.Out.WriteLine($"@ {lastEnd:hh\\:mm}, {(int)gap.TotalHours}h {gap.Minutes}m    -");
                        }
                        var comment = string.IsNullOrWhiteSpace(entry.Original.Comment) ? entry.Original.IssueSummary : entry.Original.Comment;
                        app.Out.WriteLine($"@ {entry.Start:HH\\:mm}, {(int)entry.Duration.TotalHours}h {entry.Duration.Minutes:00}m    {entry.Original.IssueId}    {comment} ");
                        lastEnd = entry.Start + entry.Duration;
                    }
                }
            });

            return await app.ExecuteAsync(args);
        }

        private static ClampedWorkEntry ClampToFifteenMinuteIntervals(WorkEntry original)
        {
            var startMinute =  Math.Round((float)original.Start.Minute / 15) * 15;
            var newStart = new DateTime(original.Start.Year, original.Start.Month, original.Start.Day, original.Start.Hour, 0, 0).AddMinutes(startMinute);
            return new ClampedWorkEntry
            {
                Start = newStart,
                Duration = original.Duration,
                Original = original,
            };
        }

        struct ClampedWorkEntry
        {
            public DateTime Start { get; set; }
            public TimeSpan Duration { get; set; }
            public WorkEntry Original { get; set; }
        }

        struct WorkEntry
        {
            public string IssueId { get; set; }
            public string IssueSummary { get; set; }
            public DateTime Start { get; set; }
            public TimeSpan Duration { get; set; }
            public string Comment { get; set; }
        }

        private static async IAsyncEnumerable<WorkEntry> GetWorklogsBetween(Jira jiraClient, string user, DateTime start, DateTime end, CancellationToken token)
        {
            var issues = await jiraClient.Issues.GetIssuesFromJqlAsync($"worklogAuthor = currentUser() AND worklogDate >= \"{start:yyyy/MM/dd}\" AND worklogDate < \"{end:yyyy/MM/dd}\"", 500, token: token);

            foreach (var issue in issues)
            {
                var worklogs = await issue.GetWorklogsAsync(token);
                foreach (var entry in worklogs)
                {
                    if (entry.StartDate == null) continue;
                    if (entry.StartDate < start) continue;
                    if (entry.StartDate >= end) continue;
                    if (!StringComparer.OrdinalIgnoreCase.Equals(entry.AuthorUser.Email, user)) continue;
                    yield return new WorkEntry
                    {
                        IssueId = issue.Key.ToString(),
                        IssueSummary = issue.Summary,
                        Start = entry.StartDate.Value,
                        Duration = TimeSpan.FromSeconds(entry.TimeSpentInSeconds),
                        Comment = entry.Comment.text,
                    };
                }
            }
        }

        private static DateTime GetDate(string dateValue)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(dateValue, "today")) return DateTime.Now;
            if (DateTime.TryParse(dateValue, out var date)) return date;
            throw new ArgumentException($"Invalid date: {dateValue}");
        }
    }
}
