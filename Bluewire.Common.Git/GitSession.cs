using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.Git.Model;
using Bluewire.Common.Git.Parsing;

namespace Bluewire.Common.Git
{
    public class GitSession
    {
        public Git Git { get; }
        private readonly IConsoleInvocationLogger logger;
        
        public GitSession(Git git, IConsoleInvocationLogger logger = null)
        {
            this.Git = git;
            this.logger = logger;
        }

        public async Task<Ref> GetCurrentBranch(GitWorkingCopy workingCopy)
        {
            var process = new CommandLine(Git.GetExecutableFilePath(), "rev-parse", "--abbrev-ref", "HEAD").RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                var currentBranchName = await GitHelpers.ExpectOneLine(process);
                var currentBranch = new Ref(currentBranchName);

                if (!Ref.IsBuiltIn(currentBranch)) return currentBranch;

                return await ResolveRef(workingCopy, currentBranch);
            }
        }

        public async Task<Ref> ResolveRef(GitWorkingCopy workingCopy, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var process = new CommandLine(Git.GetExecutableFilePath(), "rev-list", "-1", @ref.ToString()).RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                var refHash = await GitHelpers.ExpectOneLine(process);
                return new Ref(refHash);
            }
        }

        public async Task<bool> AreRefsEquivalent(GitWorkingCopy workingCopy, Ref a, Ref b)
        {
            var resolvedA = await ResolveRef(workingCopy, a);
            var resolvedB = await ResolveRef(workingCopy, b);
            return Equals(resolvedA, resolvedB);
        }

        public async Task<bool> IsClean(GitWorkingCopy workingCopy)
        {
            var process = new CommandLine(Git.GetExecutableFilePath(), "status", "--porcelain").RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                var isEmpty = process.StdOut.IsEmpty().ToTask();
                process.StdOut.StopBuffering();
                await GitHelpers.ExpectSuccess(process);
                return await isEmpty;
            }
        }

        /// <summary>
        /// Create a repository in the specified directory.
        /// </summary>
        /// <param name="containingPath">Parent directory of the new repository</param>
        /// <param name="repoName">Name of the repository</param>
        /// <returns></returns>
        public async Task<GitWorkingCopy> Init(string containingPath, string repoName)
        {
            if (containingPath == null) throw new ArgumentNullException(nameof(containingPath));
            if (repoName == null) throw new ArgumentNullException(nameof(repoName));
            if (repoName.Intersect(Path.GetInvalidFileNameChars()).Any()) throw new ArgumentException($"Repository name contains invalid characters: '{repoName}'", nameof(repoName));
            Directory.CreateDirectory(containingPath);

            var process = new CommandLine(Git.GetExecutableFilePath(), "init", repoName).RunFrom(containingPath);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }

            var workingCopy = new GitWorkingCopy(Path.Combine(containingPath, repoName));
            workingCopy.CheckExistence();
            return workingCopy;
        }

        public async Task AddFile(GitWorkingCopy workingCopy, string relativePath)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));
            if (!File.Exists(workingCopy.Path(relativePath))) throw new FileNotFoundException($"File does not exist: {relativePath}", workingCopy.Path(relativePath));

            var process = new CommandLine(Git.GetExecutableFilePath(), "add", relativePath).RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }
        }

        public async Task<GitStatusEntry[]> Status(GitWorkingCopy workingCopy)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));

            var parser = new GitStatusParser();
            var process = new CommandLine(Git.GetExecutableFilePath(), "status", "--porcelain").RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                var statusEntries = process.StdOut.Select(l => parser.ParseOrNull(l)).ToArray().ToTask();
                process.StdOut.StopBuffering();
                await GitHelpers.ExpectSuccess(process);
                WaitForCompletion(statusEntries);
                if(parser.Errors.Any())
                {
                    throw new UnexpectedGitOutputFormatException(process.CommandLine, parser.Errors.ToArray());
                }
                return await statusEntries;
            }
        }

        private static void WaitForCompletion(Task t)
        {
            if(t.IsCompleted) return;
            var asyncResult = (IAsyncResult)t;
            asyncResult.AsyncWaitHandle.WaitOne();
        }

        public async Task<Ref[]> ListBranches(GitWorkingCopy workingCopy)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));
            
            var process = new CommandLine(Git.GetExecutableFilePath(), "branch", "--list").RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                var branchNames = process.StdOut
                    .Where(l => !String.IsNullOrWhiteSpace(l))
                    .Select(l => l.Substring(2))
                    .Where(l => !l.StartsWith("(detach"))
                    .Select(l => new Ref(l))
                    .ToArray().ToTask();

                await GitHelpers.ExpectSuccess(process);
                return await branchNames;
            }
        }

        public async Task<Ref> CreateBranch(GitWorkingCopy workingCopy, string branchName, Ref start = null)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));
            start = start ?? Ref.Head;

            var branch = new Ref(branchName);
            var process = new CommandLine(Git.GetExecutableFilePath(), "branch", branch.ToString(), start.ToString()).RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
                return branch;
            }
        }

        public async Task<Ref> CreateBranchAndCheckout(GitWorkingCopy workingCopy, string branchName, Ref start = null)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));
            start = start ?? Ref.Head;

            var branch = new Ref(branchName);
            var process = new CommandLine(Git.GetExecutableFilePath(), "checkout", "-b", branch.ToString(), start.ToString()).RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
                return branch;
            }
        }

        public async Task Commit(GitWorkingCopy workingCopy, string message, CommitOptions options = 0)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));

            var cmd = new CommandLine(Git.GetExecutableFilePath(), "commit", "-m", message);
            if (options.HasFlag(CommitOptions.AllowEmptyCommit)) cmd.Add("--allow-empty");

            var process = cmd.RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }
        }

        public async Task Checkout(GitWorkingCopy workingCopy, Ref @ref)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));
            
            var process = new CommandLine(Git.GetExecutableFilePath(), "checkout", @ref.ToString()).RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }
        }

        // TODO: Better API for 'git reset'
        public async Task Reset(GitWorkingCopy workingCopy, ResetType how, Ref @ref)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));

            var option = "--" + how.ToString().ToLower();

            var process = new CommandLine(Git.GetExecutableFilePath(), "reset", option, @ref.ToString()).RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }
        }

        public async Task Merge(GitWorkingCopy workingCopy, params Ref[] @refs)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));
            
            var process = new CommandLine(Git.GetExecutableFilePath(), "merge").AddList(@refs.Select(r => r.ToString()))
                .RunFrom(workingCopy.Root);

            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }
        }

        public async Task AbortMerge(GitWorkingCopy workingCopy)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));

            var process = new CommandLine(Git.GetExecutableFilePath(), "merge", "--abort").RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }
        }
    }
}