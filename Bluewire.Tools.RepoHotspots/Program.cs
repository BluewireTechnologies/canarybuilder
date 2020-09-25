using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.GitWrapper;
using log4net.Core;

namespace Bluewire.Tools.RepoHotspots
{
    class Program
    {
        static int Main(string[] args)
        {
            var program = new Program();
            var consoleSession = new ConsoleSession()
            {
                Options =
                {
                    { "ravendb=", "Write data to the specified RavenDB instance.", (Uri o) => program.RavenDBUri = o },
                    {"repo=|repository=", "Specify the repository to use. Default: current directory.", o => program.WorkingCopyOrRepo = o},
                    { "jira=", "Use the specified Jira URL as the source of tickets.", (Uri o) => program.JiraUri = o },
                },
            };
            var logger = consoleSession.Options.AddCollector(new SimpleConsoleLoggingPolicy { Verbosity = { Default = Level.Info } });

            return consoleSession.Run(args, async () => {
                using (LoggingPolicy.Register(consoleSession, logger))
                {
                    return await program.Run();
                }
            });
        }

        public Uri RavenDBUri { get; set; }
        public Uri JiraUri { get; set; }
        public string WorkingCopyOrRepo { get; set; }

        private async Task<int> Run()
        {
            if (WorkingCopyOrRepo != null)
            {
                var logger = Log.Console.IsDebugEnabled ? new ConsoleInvocationLogger() : null;
                var git = await new GitFinder(logger).FromEnvironment();
                var gitSession = new GitSession(git, logger);
                var gitRepository = Common.GitWrapper.GitRepository.Find(WorkingCopyOrRepo);
                var job = new CollectGitCommitsAndTicketReferences(gitSession, gitRepository);

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
