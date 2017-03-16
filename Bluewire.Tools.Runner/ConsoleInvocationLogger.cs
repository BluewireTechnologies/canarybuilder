using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.Console.Logging;

namespace Bluewire.Tools.Runner
{
    class ConsoleInvocationLogger : IConsoleInvocationLogger
    {
        private static void WriteLine(string line)
        {
            Log.Console.Debug(line);
        }

        private static void WriteElapsedTime(TimeSpan elapsed)
        {
            WriteLine($"  [TIME]  {elapsed}");
        }

        public IConsoleInvocationLogScope LogInvocation(IConsoleProcess process)
        {
            WriteLine($" [SHELL]  {process.CommandLine}");

            var stdout = process.StdOut.Select(l => $"  [STDOUT]  {l}");
            var stderr = process.StdErr.Select(l => $"  [STDERR]  {l}");

            var logger = Observer.Create<string>(WriteLine);

            return new ProfilingConsoleInvocationLogScope(stdout.Merge(stderr).Subscribe(logger));
        }

        public IConsoleInvocationLogScope LogMinorInvocation(IConsoleProcess process)
        {
            return LogInvocation(process);
        }

        class ProfilingConsoleInvocationLogScope : ConsoleInvocationLogScope
        {
            private readonly Stopwatch stopwatch;

            public ProfilingConsoleInvocationLogScope(params IDisposable[] subscriptions) : base(subscriptions)
            {
                stopwatch = Stopwatch.StartNew();
            }

            public override void Dispose()
            {
                WriteElapsedTime(stopwatch.Elapsed);
                base.Dispose();
            }
        }
    }
}
