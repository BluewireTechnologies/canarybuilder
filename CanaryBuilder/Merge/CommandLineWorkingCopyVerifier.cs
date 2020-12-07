using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using CanaryBuilder.Logging;
using CliWrap;

namespace CanaryBuilder.Merge
{
    public class CommandLineWorkingCopyVerifier : IWorkingCopyVerifier
    {
        public CommandLineWorkingCopyVerifier(Command command)
        {
            Command = command;
        }

        public Command Command { get; }

        public async Task Verify(GitWorkingCopy workingCopy, IJobLogger details)
        {
            var result = await Command
                .RunFrom(workingCopy)
                .LogInvocation(details, out var log)
                .ExecuteAsync()
                .LogResult(log);

            if (result.ExitCode != 0) throw new InvalidWorkingCopyStateException($"The command exited with code {result.ExitCode}");
        }
    }
}
