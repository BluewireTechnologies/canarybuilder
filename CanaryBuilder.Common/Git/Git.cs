using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CanaryBuilder.Common.Shell;

namespace CanaryBuilder.Common.Git
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

        public async Task Validate()
        {
            // check that the binary can execute
            await GetVersionString();
        }

        public async Task<string> GetVersionString()
        {
            var process = new CommandLine(exePath, "--version").RunFrom(GetExecutableDirectory());

            var versionString = await GitHelpers.ExpectOneLine(process); ;
            const string expectedPrefix = "git version ";
            if (!versionString.StartsWith(expectedPrefix)) throw new UnexpectedGitOutputFormatException(process.CommandLine);

            return versionString.Substring(expectedPrefix.Length).Trim();
        }
    }
}