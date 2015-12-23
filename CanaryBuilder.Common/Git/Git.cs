using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var commandLine = new CommandLine(exePath, "--version");
            
            var versionString = (await ReadStdoutFromInvocation(GetExecutableDirectory(), commandLine)).FirstOrDefault(l => !String.IsNullOrWhiteSpace(l));
            if (versionString == null) throw new UnexpectedGitOutputFormatException(commandLine);
            const string expectedPrefix = "git version ";
            if (!versionString.StartsWith(expectedPrefix)) throw new UnexpectedGitOutputFormatException(commandLine);

            return versionString.Substring(expectedPrefix.Length).Trim();
        }

        private static async Task<IEnumerable<string>> ReadStdoutFromInvocation(string workingDirectory, CommandLine commandLine)
        {
            var process = new CommandLineInvoker(workingDirectory).Start(commandLine);

            var code = await process.Completed;
            if (code != 0) throw new GitException(commandLine, code, String.Join(Environment.NewLine, process.StdErr.ToEnumerable()));

            return process.StdOut.ToEnumerable();
        }

        public async Task<Ref> GetCurrentBranch(GitWorkingCopy workingCopy)
        {
            var getHeadRefCmd = new CommandLine(exePath, "rev-parse", "--abbrev-ref", "HEAD");

            var currentBranchName = (await ReadStdoutFromInvocation(workingCopy.Root, getHeadRefCmd)).FirstOrDefault(l => !String.IsNullOrWhiteSpace(l));
            if (currentBranchName == null) throw new UnexpectedGitOutputFormatException(getHeadRefCmd);

            // Should maybe check if it's a builtin of any sort, rather than just HEAD?
            if (currentBranchName != "HEAD")
            {
                return new Ref(currentBranchName);
            }
            return await ResolveRef(workingCopy, new Ref(currentBranchName));
        }

        public async Task<Ref> ResolveRef(GitWorkingCopy workingCopy, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var resolveRefCmd = new CommandLine(exePath, "rev-list", "-1", @ref.ToString());
            var refHash = (await ReadStdoutFromInvocation(workingCopy.Root, resolveRefCmd)).FirstOrDefault(l => !String.IsNullOrWhiteSpace(l));
            if (refHash == null) throw new UnexpectedGitOutputFormatException(resolveRefCmd);
            return new Ref(refHash);
        }

        public async Task<bool> IsClean(GitWorkingCopy workingCopy)
        {
            var getModifiedPathsCmd = new CommandLine(exePath, "status", "--porcelain");

            var modified = await ReadStdoutFromInvocation(workingCopy.Root, getModifiedPathsCmd);
            return !modified.Any();
        }
    }


}