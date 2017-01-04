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
        public GitCommandHelper CommandHelper { get; }

        public GitSession(Git git, IConsoleInvocationLogger logger = null)
        {
            this.Git = git;
            this.logger = logger;

            CommandHelper = new GitCommandHelper(git, logger);
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

        public async Task<bool> ExactRefExists(IGitFilesystemContext workingCopyOrRepo, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var process = workingCopyOrRepo.Invoke(new CommandLine(Git.GetExecutableFilePath(), "show-ref", "--verify", "--quiet", @ref));
            using (var log = logger?.LogMinorInvocation(process))
            {
                log?.IgnoreExitCode();

                var exitCode = await process.Completed;
                return exitCode == 0;
            }
        }

        public async Task<bool> BranchExists(IGitFilesystemContext workingCopyOrRepo, Ref branch)
        {
            if (branch == null) throw new ArgumentNullException(nameof(branch));

            return await ExactRefExists(workingCopyOrRepo, RefHelper.PutInHierarchy("heads", branch));
        }

        public async Task<bool> TagExists(IGitFilesystemContext workingCopyOrRepo, Ref tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            return await ExactRefExists(workingCopyOrRepo, RefHelper.PutInHierarchy("tags", tag));
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

        /// <summary>
        /// Clone a repository to the specified directory.
        /// </summary>
        /// <param name="remote">Uri of the repository to be cloned</param>
        /// <param name="containingPath">Parent directory of the new repository</param>
        /// <param name="repoName">Name of the repository</param>
        /// <returns></returns>
        public async Task<GitWorkingCopy> Clone(Uri remote, string containingPath, string repoName)
        {
            if (remote == null) throw new ArgumentNullException(nameof(remote));
            if (containingPath == null) throw new ArgumentNullException(nameof(containingPath));
            if (repoName == null) throw new ArgumentNullException(nameof(repoName));
            if (repoName.Intersect(Path.GetInvalidFileNameChars()).Any()) throw new ArgumentException($"Repository name contains invalid characters: '{repoName}'", nameof(repoName));
            Directory.CreateDirectory(containingPath);

            var process = new CommandLine(Git.GetExecutableFilePath(), "clone", remote.ToString(), repoName).RunFrom(containingPath);
            using (logger?.LogInvocation(process))
            {
                process.StdOut.StopBuffering();

                await GitHelpers.ExpectSuccess(process);
            }

            var workingCopy = new GitWorkingCopy(Path.Combine(containingPath, repoName));
            workingCopy.CheckExistence();
            return workingCopy;
        }

        /// <summary>
        /// Fetch into the repository from a named remote.
        /// </summary>
        public async Task Fetch(IGitFilesystemContext workingCopyOrRepo, string remoteName = null)
        {
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, "fetch", remoteName);
        }

        public async Task AddFile(GitWorkingCopy workingCopy, string relativePath)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));
            if (!File.Exists(workingCopy.Path(relativePath))) throw new FileNotFoundException($"File does not exist: {relativePath}", workingCopy.Path(relativePath));

            await CommandHelper.RunSimpleCommand(workingCopy, "add", relativePath);
        }

        public async Task<GitStatusEntry[]> Status(GitWorkingCopy workingCopy)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));

            var parser = new GitStatusParser();
            var process = new CommandLine(Git.GetExecutableFilePath(), "status", "--porcelain").RunFrom(workingCopy.Root);
            return await CommandHelper.ParseLineOutput(process, parser);
        }

        public async Task<Ref[]> ListBranches(IGitFilesystemContext workingCopyOrRepo, ListBranchesOptions options = default(ListBranchesOptions))
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var cmd = new CommandLine(Git.GetExecutableFilePath(), "branch", "--list");
            if (options.Remote) cmd.Add("--remotes");
            if (options.UnmergedWith != null) cmd.Add("--no-merged", options.UnmergedWith);
            if (options.MergedWith != null) cmd.Add("--merged", options.MergedWith);

            var process = workingCopyOrRepo.Invoke(cmd);
            using (logger?.LogInvocation(process))
            {
                var branchNames = process.StdOut
                    .Where(l => !String.IsNullOrWhiteSpace(l))
                    .Select(l => l.Substring(2))
                    .Where(l => !l.StartsWith("(detach") && !l.Contains(" -> ") && !l.StartsWith("(HEAD detached "))
                    .Select(l => new Ref(l))
                    .ToArray().ToTask();

                await GitHelpers.ExpectSuccess(process);
                return await branchNames;
            }
        }
        
        public async Task<Ref> CreateBranch(IGitFilesystemContext workingCopyOrRepo, string branchName, Ref start = null)
        {
            var branch = new Ref(branchName);
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, "branch", branch, start ?? Ref.Head);
            return branch;
        }

        public async Task<Ref[]> ListTags(IGitFilesystemContext workingCopyOrRepo, ListTagsOptions options = default(ListTagsOptions))
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var cmd = new CommandLine(Git.GetExecutableFilePath(), "tag", "--list");
            if (options.Contains != null) cmd.Add("--contains", options.Contains);
            if (!String.IsNullOrWhiteSpace(options.Pattern)) cmd.Add(options.Pattern);

            var process = workingCopyOrRepo.Invoke(cmd);
            using (logger?.LogInvocation(process))
            {
                var tagNames = process.StdOut
                    .Where(l => !String.IsNullOrWhiteSpace(l))
                    .Select(l => new Ref(l))
                    .ToArray().ToTask();

                await GitHelpers.ExpectSuccess(process);
                return await tagNames;
            }
        }

        public async Task<Ref> CreateTag(IGitFilesystemContext workingCopyOrRepo, string tagName, Ref tagLocation, string message, bool force = false)
        {
            if (tagLocation == null) throw new ArgumentNullException(nameof(tagLocation));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var tag = new Ref(tagName);
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, "tag", tag,
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
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, "tag", "--annotate", tag,
                force ? "--force" : null,
                "--message", message,
                tagLocation);

            return tag;
        }

        public async Task DeleteTag(IGitFilesystemContext workingCopyOrRepo, Ref tag)
        {
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, "tag", "--delete", tag);
        }

        public async Task<TagDetails> ReadTagDetails(IGitFilesystemContext workingCopyOrRepo, Ref tag)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var parser = new GitTagDetailsParser();
            var process = workingCopyOrRepo.Invoke(new CommandLine(Git.GetExecutableFilePath(), "cat-file", "tag", tag));
            using (logger?.LogInvocation(process))
            {
                await GitHelpers.ExpectSuccess(process);
                // Not expecting large amounts of data, so just buffer it all:
                var lines = await process.StdOut.ReadAllLinesAsync();
                var details = parser.Parse(lines);
                if (parser.Errors.Any())
                {
                    throw new UnexpectedGitOutputFormatException(process.CommandLine, parser.Errors.ToArray());
                }
                return details;
            }
        }

        public async Task<Ref> CreateBranchAndCheckout(GitWorkingCopy workingCopy, string branchName, Ref start = null)
        {
            var branch = new Ref(branchName);
            await CommandHelper.RunSimpleCommand(workingCopy, "checkout", "-b", branch, start ?? Ref.Head);
            return branch;
        }

        public async Task DeleteBranch(IGitFilesystemContext workingCopyOrRepo, Ref branch, bool force = false)
        {
            // I'm currently running Git v1.9.5, which doesn't understand 'branch --delete --force'
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, "branch", force ? "-D" : "--delete", branch);
        }

        public async Task Commit(GitWorkingCopy workingCopy, string message, CommitOptions options = 0)
        {
            await CommandHelper.RunSimpleCommand(workingCopy, "commit",
                options.HasFlag(CommitOptions.AllowEmptyCommit) ? "--allow-empty" : null,
                 "--message", message);
        }

        public async Task Checkout(GitWorkingCopy workingCopy, Ref @ref)
        {
            await CommandHelper.RunSimpleCommand(workingCopy, "checkout", @ref);
        }

        public async Task CheckoutCompletelyClean(GitWorkingCopy workingCopy, Ref @ref = null)
        {
            await CommandHelper.RunSimpleCommand(workingCopy, "checkout", "--force", @ref);
            await CommandHelper.RunSimpleCommand(workingCopy, "clean", "--force", "-xd");
        }

        public async Task ResetCompletelyClean(GitWorkingCopy workingCopy, Ref @ref = null)
        {
            await CommandHelper.RunSimpleCommand(workingCopy, "reset", "--hard", @ref);
            await CommandHelper.RunSimpleCommand(workingCopy, "clean", "--force", "-xd");
        }

        // TODO: Better API for 'git reset'
        public async Task Reset(GitWorkingCopy workingCopy, ResetType how, Ref @ref)
        {
            var option = "--" + how.ToString().ToLower();

            await CommandHelper.RunSimpleCommand(workingCopy, "reset", option, @ref);
        }

        public Task Merge(GitWorkingCopy workingCopy, params Ref[] @refs)
        {
            return Merge(workingCopy, default(MergeOptions), @refs);
        }

        public async Task Merge(GitWorkingCopy workingCopy, MergeOptions options, params Ref[] @refs)
        {
            var cmd = CommandHelper.CreateCommand("merge");
            if (options.FastForward == MergeFastForward.Never) cmd.Add("--no-ff");
            else if (options.FastForward == MergeFastForward.Only) cmd.Add("--ff-only");

            cmd.AddList(@refs.Select(r => r.ToString()));

            await CommandHelper.RunSimpleCommand(workingCopy, cmd);
        }

        public async Task AbortMerge(GitWorkingCopy workingCopy)
        {
            await CommandHelper.RunSimpleCommand(workingCopy, "merge", "--abort");
        }
    }
}
