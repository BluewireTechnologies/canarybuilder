using System.IO;
using CliWrap;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    class TestConsoleInvocationLogger : IConsoleInvocationLogger, IConsoleInvocationLogScope
    {
        private readonly TextWriter log;

        public TestConsoleInvocationLogger(TextWriter log)
        {
            this.log = log;
        }

        private void WriteLine(string line)
        {
            lock (this) log.WriteLine(line);
        }

        public IConsoleInvocationLogScope Create() => this;
        public IConsoleInvocationLogScope CreateMinor() => this;

        Command IConsoleInvocationLogScope.LogOutputs(Command command)
        {
            return command
                .TeeStandardOutput(l => WriteLine($"  [Out]  {l}"))
                .TeeStandardError(l => WriteLine($"  [Err]  {l}"));
        }

        void IConsoleInvocationLogScope.LogResult(CommandResult result, bool ignoreExitCode) { }
    }
}
