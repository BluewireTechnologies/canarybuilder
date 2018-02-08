using System;
using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;

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
            await GetVersionString(logger);
        }

        public async Task<string> GetVersionString(IConsoleInvocationLogger logger = null)
        {
            var process = new CommandLine(exePath, "--version").RunFrom(GetExecutableDirectory());
            using (logger?.LogMinorInvocation(process))
            {
                var versionString = await GitHelpers.ExpectOneLine(process); ;
                const string expectedPrefix = "git version ";
                if (!versionString.StartsWith(expectedPrefix)) throw new UnexpectedGitOutputFormatException(process.CommandLine);

                return versionString.Substring(expectedPrefix.Length).Trim();
            }
        }
    }
}
