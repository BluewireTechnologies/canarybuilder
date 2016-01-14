using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Common.GitWrapper.Parsing;

namespace Bluewire.Common.GitWrapper
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
            using (logger?.LogMinorInvocation(process))
            {
                var currentBranchName = await GitHelpers.ExpectOneLine(process);
                var currentBranch = new Ref(currentBranchName);

                if (!Ref.IsBuiltIn(currentBranch)) return currentBranch;

                return await ResolveRef(workingCopy, currentBranch);
            }
        }

        public async Task<Ref> ResolveRef(IGitFilesystemContext workingCopyOrRepo, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var process = workingCopyOrRepo.Invoke(new CommandLine(Git.GetExecutableFilePath(), "rev-list", "--max-count=1", @ref));
            using (logger?.LogMinorInvocation(process))
            {
                var refHash = await GitHelpers.ExpectOneLine(process);
                return new Ref(refHash);
            }
        }

        public async Task<bool> RefExists(IGitFilesystemContext workingCopyOrRepo, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var process = workingCopyOrRepo.Invoke(new CommandLine(Git.GetExecutableFilePath(), "show-ref", "--quiet", @ref));
            using (var log = logger?.LogMinorInvocation(process))
            {
                log?.IgnoreExitCode();

                var exitCode = await process.Completed;
                return exitCode == 0;
            }
        }

        public async Task<bool> AreRefsEquivalent(IGitFilesystemContext workingCopyOrRepo, Ref a, Ref b)
        {
            var resolvedA = await ResolveRef(workingCopyOrRepo, a);
            var resolvedB = await ResolveRef(workingCopyOrRepo, b);
            return Equals(resolvedA, resolvedB);
        }

        public async Task<bool> IsClean(GitWorkingCopy workingCopy)
        {
            var process = new CommandLine(Git.GetExecutableFilePath(), "status", "--porcelain").RunFrom(workingCopy.Root);
            using (logger?.LogMinorInvocation(process))
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

            await RunSimpleCommand(workingCopy, "add", relativePath);
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
                if (parser.Errors.Any())
                {
                    throw new UnexpectedGitOutputFormatException(process.CommandLine, parser.Errors.ToArray());
                }
                return await statusEntries;
            }
        }

        private static void WaitForCompletion(Task t)
        {
            if (t.IsCompleted) return;
            var asyncResult = (IAsyncResult)t;
            asyncResult.AsyncWaitHandle.WaitOne();
        }

        public async Task<Ref[]> ListBranches(IGitFilesystemContext workingCopyOrRepo)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var process = workingCopyOrRepo.Invoke(new CommandLine(Git.GetExecutableFilePath(), "branch", "--list"));
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

        public async Task<Ref> CreateBranch(IGitFilesystemContext workingCopyOrRepo, string branchName, Ref start = null)
        {
            var branch = new Ref(branchName);
            await RunSimpleCommand(workingCopyOrRepo, "branch", branch, start ?? Ref.Head);
            return branch;
        }

        public async Task<Ref> CreateTag(IGitFilesystemContext workingCopyOrRepo, string tagName, Ref tagLocation, string message, bool force = false)
        {
            if (tagLocation == null) throw new ArgumentNullException(nameof(tagLocation));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var tag = new Ref(tagName);
            await RunSimpleCommand(workingCopyOrRepo, "tag", tag,
                force ? "--force" : null,
                "--message", message,
                tagLocation);

            return tag;
        }

        public async Task<Ref> CreateAnnotatedTag(IGitFilesystemContext workingCopyOrRepo, string tagName, Ref tagLocation, string message, bool force = false)
        {
            if (tagLocation == null) throw new ArgumentNullException(nameof(tagLocation));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var tag = new Ref(tagName);
            await RunSimpleCommand(workingCopyOrRepo, "tag", "--annotate", tag,
                force ? "--force" : null,
                "--message", message,
                tagLocation);

            return tag;
        }

        public async Task DeleteTag(IGitFilesystemContext workingCopyOrRepo, Ref tag)
        {
            await RunSimpleCommand(workingCopyOrRepo, "tag", "--delete", tag);
        }

        public async Task<Ref> CreateBranchAndCheckout(GitWorkingCopy workingCopy, string branchName, Ref start = null)
        {
            var branch = new Ref(branchName);
            await RunSimpleCommand(workingCopy, "checkout", "-b", branch, start ?? Ref.Head);
            return branch;
        }

        public async Task DeleteBranch(IGitFilesystemContext workingCopyOrRepo, Ref branch, bool force = false)
        {
            // I'm currently running Git v1.9.5, which doesn't understand 'branch --delete --force'
            await RunSimpleCommand(workingCopyOrRepo, "branch", force ? "-D" : "--delete", branch);
        }

        public async Task Commit(GitWorkingCopy workingCopy, string message, CommitOptions options = 0)
        {
            await RunSimpleCommand(workingCopy, "commit",
                options.HasFlag(CommitOptions.AllowEmptyCommit) ? "--allow-empty" : null,
                 "--message", message);
        }

        public async Task Checkout(GitWorkingCopy workingCopy, Ref @ref)
        {
            await RunSimpleCommand(workingCopy, "checkout", @ref);
        }

        public async Task CheckoutCompletelyClean(GitWorkingCopy workingCopy, Ref @ref = null)
        {
            await RunSimpleCommand(workingCopy, "checkout", "--force", @ref);
            await RunSimpleCommand(workingCopy, "clean", "--force", "-xd", @ref);
        }

        public async Task ResetCompletelyClean(GitWorkingCopy workingCopy, Ref @ref = null)
        {
            await RunSimpleCommand(workingCopy, "reset", "--hard", @ref);
            await RunSimpleCommand(workingCopy, "clean", "--force", "-xd", @ref);
        }

        // TODO: Better API for 'git reset'
        public async Task Reset(GitWorkingCopy workingCopy, ResetType how, Ref @ref)
        {
            var option = "--" + how.ToString().ToLower();

            await RunSimpleCommand(workingCopy, "reset", option, @ref);
        }

        public Task Merge(GitWorkingCopy workingCopy, params Ref[] @refs)
        {
            return Merge(workingCopy, default(MergeOptions), @refs);
        }

        public async Task Merge(GitWorkingCopy workingCopy, MergeOptions options, params Ref[] @refs)
        {
            await RunSimpleCommand(workingCopy, "merge", c =>
            {
                if (options.FastForward == MergeFastForward.Never) c.Add("--no-ff");
                else if (options.FastForward == MergeFastForward.Only) c.Add("--ff-only");

                c.AddList(@refs.Select(r => r.ToString()));
            });
        }

        public async Task AbortMerge(GitWorkingCopy workingCopy)
        {
            await RunSimpleCommand(workingCopy, "merge", "--abort");
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to simply succeed or fail. Output is ignored.
        /// </summary>
        private Task RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, string gitCommand, params string[] arguments)
        {
            return RunSimpleCommand(workingCopyOrRepo, gitCommand, c => c.Add(arguments));
        }

        /// <summary>
        /// Helper method. Runs a command which is expected to simply succeed or fail. Output is ignored.
        /// </summary>
        private async Task RunSimpleCommand(IGitFilesystemContext workingCopyOrRepo, string gitCommand, Action<CommandLine> prepareCommand)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var cmd = new CommandLine(Git.GetExecutableFilePath(), gitCommand);
            prepareCommand(cmd);
            var process = workingCopyOrRepo.Invoke(cmd);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }
        }
    }
}