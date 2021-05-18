using System;
using System.IO;
using Bluewire.Common.GitWrapper;
using CliWrap;

namespace Bluewire.Stash.Tool
{
    public class VerboseLogger
    {
        private readonly TextWriter writer;
        private readonly int specifiedLevel;

        public VerboseLogger(TextWriter writer, int specifiedLevel)
        {
            this.writer = writer;
            this.specifiedLevel = specifiedLevel;
        }

        public bool IsEnabled(int level) => specifiedLevel >= level;

        public void WriteLine(int level, string message)
        {
            if (!IsEnabled(level)) return;
            writer.WriteLine(message);
        }

        public void WriteLine(int level, Func<string> message)
        {
            if (!IsEnabled(level)) return;
            writer.WriteLine(message());
        }

        public IConsoleInvocationLogger? GetConsoleInvocationLogger(int level)
        {
            if (!IsEnabled(level)) return null;
            return new ConsoleInvocationLogger(writer);
        }

        class ConsoleInvocationLogger : IConsoleInvocationLogger, IConsoleInvocationLogScope
        {
            private readonly TextWriter writer;

            public ConsoleInvocationLogger(TextWriter writer)
            {
                this.writer = writer;
            }

            private void WriteLine(string line) => writer.WriteLine(line);

            private void WriteElapsedTime(TimeSpan elapsed) => WriteLine($"  [TIME]  {elapsed}");

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
}
