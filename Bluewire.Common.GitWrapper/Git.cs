using System;
using System.IO;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Wraps invocation of the Git binary.
    /// </summary>
    public class Git
    {
        private readonly string exePath;

        public Git(string exePath)
        {
            if (exePath == null) throw new ArgumentNullException(nameof(exePath));
            if (!Path.IsPathRooted(exePath)) throw new ArgumentException($"Not an absolute path: {exePath}", nameof(exePath));
            this.exePath = exePath;
        }

        public string GetExecutableFilePath() => exePath;
        private string GetExecutableDirectory() => Path.Combine(Path.GetPathRoot(exePath), Path.GetDirectoryName(exePath));

        public async Task Validate(IConsoleInvocationLogger logger = null)
        {
            // check that the binary can execute
            await GetVersionString(logger)
                .ConfigureAwait(false);
        }

        public async Task<string> GetVersionString(IConsoleInvocationLogger logger = null)
        {
            var command = Cli.Wrap(exePath)
                .WithValidation(CommandResultValidation.None)
                .WithArguments("--version");

            var result = await command
                .WithWorkingDirectory(GetExecutableDirectory())
                .LogMinorInvocation(logger, out var log)
                .ExecuteBufferedAsync()
                .LogResult(log);

            var versionString = GitHelpers.ExpectOneLine(command, result);
            const string expectedPrefix = "git version ";
            if (!versionString.StartsWith(expectedPrefix)) throw new UnexpectedGitOutputFormatException(command);

            return versionString.Substring(expectedPrefix.Length).Trim();
        }
    }
}
