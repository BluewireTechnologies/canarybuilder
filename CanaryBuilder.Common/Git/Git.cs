using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            this.exePath = exePath;
        }

        public string GetExecutableFilePath() => exePath;

        public async Task Validate()
        {
            // check that the binary can execute
            await GetVersionString();
        }

        public async Task<string> GetVersionString()
        {
            var commandLine = new CommandLine(exePath, "--version");

            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var code = await new CommandLineInvoker().Run(commandLine, CancellationToken.None, stdout, stderr);
            if (code != 0) throw new GitException(commandLine, code, stderr.ToString());

            var versionString = AsNativeLines(stdout).FirstOrDefault(l => !String.IsNullOrWhiteSpace(l));
            if (versionString == null) throw new UnexpectedGitOutputFormatException(commandLine);
            const string expectedPrefix = "git version ";
            if (!versionString.StartsWith(expectedPrefix)) throw new UnexpectedGitOutputFormatException(commandLine);

            return versionString.Substring(expectedPrefix.Length).Trim();
        }

        private static IEnumerable<string> AsNativeLines(StringWriter writer)
        {
            return writer.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}