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
using Bluewire.Common.Console.ThirdParty;
using Bluewire.Common.GitWrapper.Model;
using log4net.Core;

namespace RefCleaner
{
    class Program
    {
        static int Main(string[] args)
        {
            var arguments = new Arguments();
            var options = new OptionSet
            {
                { "repo=|repository=", "Use the specified repository as the source of tag and branch information.", o => arguments.RepositoryPath = o },
                { "remote=", "Target the specified remote for cleanup", o => arguments.RemoteName = o },
                // Not implemented:
                { "aggressive", "Include all branches which are not identified as 'must keep', rather than just those marked 'discardable'.", o => arguments.Aggressive = true }
            };

            var session = new ConsoleSession<Arguments>(arguments, options);

            return session.Run(args, async a => await new Program(a).Run());
        }

        private readonly Arguments arguments;

        private Program(Arguments arguments)
        {
            this.arguments = arguments;
            Log.Configure();
            Log.SetConsoleVerbosity(arguments.Verbosity);
        }

        private async Task<int> Run()
        {
            var factory = new RefCollectorFactory(arguments.RepositoryPath, arguments.RemoteName, Log.Console.IsDebugEnabled ? new ConsoleInvocationLogger() : null);

            var collectors = new List<IRefCollector>
            {
                await factory.CreateExpiredCanaryTagCollector(),
                await factory.CreateBranchCollectors()
            };

            var refs = new List<Ref>();
            foreach (var collector in collectors)
            {
                refs.AddRange(await collector.CollectRefs());
            }
            
            foreach (var @ref in refs.Distinct())
            {
                Console.Out.WriteLine(@ref);
            }
            
            return 0;
        }

        class Arguments : IVerbosityArgument
        {
            private readonly IEnumerable<Level> logLevels = new List<Level> { Level.Warn, Level.Info, Level.Debug };
            private int logLevel = 1;

            public Level Verbosity => logLevels.ElementAtOrDefault(logLevel) ?? Level.All;

            public void Verbose()
            {
                logLevel++;
            }
            
            public string RepositoryPath { get; set; }
            public string RemoteName { get; set; }
            public bool Aggressive { get; set; }
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
