using System;
using Bluewire.Common.Console.Logging;
using Bluewire.Common.GitWrapper;
using CliWrap;

namespace Bluewire.Tools.Runner
{
    class ConsoleInvocationLogger : IConsoleInvocationLogger, IConsoleInvocationLogScope
    {
        private static void WriteLine(string line)
        {
            Log.Console.Debug(line);
        }

        private static void WriteElapsedTime(TimeSpan elapsed)
        {
            WriteLine($"  [TIME]  {elapsed}");
        }

        public IConsoleInvocationLogScope Create() => this;
        public  IConsoleInvocationLogScope CreateMinor() => this;

        Command IConsoleInvocationLogScope.LogOutputs(Command command)
        {
            WriteLine($" [SHELL]  {command}");

            return command
                .TeeStandardOutput(l => WriteLine($"  [STDOUT]  {l}"))
                .TeeStandardError(l => WriteLine($"  [STDERR]  {l}"));
        }

        void IConsoleInvocationLogScope.LogResult(CommandResult result, bool ignoreExitCode)
        {
            WriteElapsedTime(result.RunTime);
        }
    }
}
