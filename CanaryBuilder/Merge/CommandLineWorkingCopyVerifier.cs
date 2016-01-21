using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper;
using CanaryBuilder.Logging;

namespace CanaryBuilder.Merge
{
    public class CommandLineWorkingCopyVerifier : IWorkingCopyVerifier
    {
        public CommandLineWorkingCopyVerifier(ICommandLine commandLine)
        {
            CommandLine = commandLine.Seal();
        }

        public ICommandLine CommandLine { get; }

        public async Task Verify(GitWorkingCopy workingCopy, IJobLogger details)
        {
            var process = CommandLine.RunFrom(workingCopy.Root);
            using (details.LogInvocation(process))
            {
                var exitCode = await process.Completed;
                if (exitCode != 0) throw new InvalidWorkingCopyStateException($"The command exited with code {exitCode}");
            }
        }
    }
}
