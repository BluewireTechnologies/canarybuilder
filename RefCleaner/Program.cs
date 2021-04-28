using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using CliWrap;
using log4net.Core;

namespace RefCleaner
{
    class Program
    {
        static int Main(string[] args)
        {
            var program = new Program();
            var session = new ConsoleSession
            {
                Options = {
                    { "repo=|repository=", "Use the specified repository as the source of tag and branch information.", o => program.RepositoryPath = o },
                    { "remote=", "Target the specified remote for cleanup", o => program.RemoteName = o },
                    // Not implemented:
                    { "aggressive", "Include all branches which are not identified as 'must keep', rather than just those marked 'discardable'.", o => program.Aggressive = true }
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

        public string RepositoryPath { get; set; }
        public string RemoteName { get; set; }
        public bool Aggressive { get; set; }

        private async Task<int> Run()
        {
            var factory = new RefCollectorFactory(RepositoryPath, RemoteName, Log.Console.IsDebugEnabled ? new ConsoleInvocationLogger() : null);

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

        class ConsoleInvocationLogger : IConsoleInvocationLogger, IConsoleInvocationLogScope
        {
            private void WriteLine(string line)
            {
                Log.Console.Debug(line);
            }

            public IConsoleInvocationLogScope Create() => this;
            public IConsoleInvocationLogScope CreateMinor() => this;

            Command IConsoleInvocationLogScope.LogOutputs(Command command)
            {
                return command
                    .TeeStandardOutput(l => WriteLine($"  [STDOUT]  {l}"))
                    .TeeStandardError(l => WriteLine($"  [STDERR]  {l}"));
            }

            void IConsoleInvocationLogScope.LogResult(CommandResult result, bool ignoreExitCode) { }
        }
    }
}
