using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using CanaryBuilder.Common.Git.Model;
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
            
            var versionString = await GitHelpers.ExpectOneLine(process);;
            const string expectedPrefix = "git version ";
            if (!versionString.StartsWith(expectedPrefix)) throw new UnexpectedGitOutputFormatException(process.CommandLine);

            return versionString.Substring(expectedPrefix.Length).Trim();
        }

        private static async Task<IEnumerable<string>> ReadStdoutFromInvocation(string workingDirectory, CommandLine commandLine)
        {
            var process = new CommandLineInvoker(workingDirectory).Start(commandLine);

            var code = await process.Completed;
            if (code != 0) throw new GitException(commandLine, code, String.Join(Environment.NewLine, await process.StdErr.ToStringAsync()));

            return process.StdOut.ToEnumerable();
        }

        public async Task<Ref> GetCurrentBranch(GitWorkingCopy workingCopy)
        {
            var process = new CommandLine(exePath, "rev-parse", "--abbrev-ref", "HEAD").RunFrom(workingCopy.Root);

            var currentBranchName = await GitHelpers.ExpectOneLine(process);

            // Should maybe check if it's a builtin of any sort, rather than just HEAD?
            if (currentBranchName != "HEAD") return new Ref(currentBranchName);

            return await ResolveRef(workingCopy, new Ref(currentBranchName));
        }

        public async Task<Ref> ResolveRef(GitWorkingCopy workingCopy, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var process = new CommandLine(exePath, "rev-list", "-1", @ref.ToString()).RunFrom(workingCopy.Root);
            var refHash = await GitHelpers.ExpectOneLine(process);
            return new Ref(refHash);
        }

        public async Task<bool> IsClean(GitWorkingCopy workingCopy)
        {
            var process = new CommandLine(exePath, "status", "--porcelain").RunFrom(workingCopy.Root);
            return ! await process.StdOut.StopBuffering().Any().SingleOrDefaultAsync();
        }
    }
}