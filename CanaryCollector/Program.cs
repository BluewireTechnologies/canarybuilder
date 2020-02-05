using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.Console.Logging;
using CanaryCollector.Collectors;
using CanaryCollector.Model;
using CanaryCollector.Remote;
using CanaryCollector.Remote.Jira;
using log4net.Core;

namespace CanaryCollector
{
    class Program
    {
        static int Main(string[] args)
        {
            var program = new Program();
            var session = new ConsoleSession
            {
                Options = {
                    { "pending", "Include tickets pending review.", o => program.IncludePending = true },
                    { "tag=", "Include tickets with the specified tag.", o => program.IncludeTags.Add(o) },
                    { "url=", "Include branches listed by the specified resource. Currently only Google Docs spreadsheets are supported.", (Uri o) => program.IncludeUris.Add(o) },
                    { "jira=", "Use the specified Jira URL as the source of tickets.", (Uri o) => program.JiraUri = o },
                    { "repo=|repository=", "Use the specified repository as the source of branches.", o => program.RepositoryPath = o }
                }
            };
            var logger = session.Options.AddCollector(new SimpleConsoleLoggingPolicy { Verbosity = { Default = Level.Info } });

            return session.Run(args, async () => {
                using (LoggingPolicy.Register(session, logger))
                {
                    return await program.Run();
                }
            });
        }

        public ICollection<string> IncludeTags { get; } = new HashSet<string>();
        public ICollection<Uri> IncludeUris { get; } = new HashSet<Uri>();
        public bool IncludePending { get; set; }

        public Uri JiraUri { get; set; }
        public string RepositoryPath { get; set; }

        private async Task<int> Run()
        {
            var factory = new BranchCollectorFactory(GetTicketProviderFactory(), RepositoryPath, Log.Console.IsDebugEnabled ? new ConsoleInvocationLogger() : null);

            var collectors = new List<IBranchCollector>();
            collectors.AddRange(factory.CreateUriCollectors(IncludeUris));
            collectors.AddRange(await factory.CreateTagCollectors(IncludeTags));
            if (IncludePending) collectors.Add(await factory.CreatePendingCollector());
            if (!collectors.Any()) throw new InvalidArgumentsException("Nothing to do. Please specify --pending, --tag or --uri.");

            var branches = new List<Branch>();
            foreach (var collector in collectors)
            {
                branches.AddRange(await collector.CollectBranches());
            }

            foreach (var branch in DeduplicateBranchesByName(branches))
            {
                Console.Out.WriteLine(branch.Name);
            }

            return 0;
        }

        private ITicketProviderFactory GetTicketProviderFactory()
        {
            if (JiraUri != null) return new JiraTicketProviderFactory(JiraUri);
            return new NoTicketProviderFactory();
        }

        private IEnumerable<Branch> DeduplicateBranchesByName(IEnumerable<Branch> branches)
        {
            var seen = new HashSet<string>();
            foreach (var branch in branches)
            {
                if (seen.Add(branch.Name))
                {
                    yield return branch;
                }
            }
        }

        class ConsoleInvocationLogger : IConsoleInvocationLogger
        {
            private void WriteLine(string line)
            {
                Log.Console.Debug(line);
            }

            public IConsoleInvocationLogScope LogInvocation(IConsoleProcess process)
            {
                WriteLine($" [SHELL]  {process.CommandLine}");

                var stdout = process.StdOut.Select(l => $"  [STDOUT]  {l}");
                var stderr = process.StdErr.Select(l => $"  [STDERR]  {l}");

                var logger = Observer.Create<string>(WriteLine);

                return new ConsoleInvocationLogScope(stdout.Merge(stderr).Subscribe(logger));
            }

            public IConsoleInvocationLogScope LogMinorInvocation(IConsoleProcess process)
            {
                return LogInvocation(process);
            }
        }
    }
}
