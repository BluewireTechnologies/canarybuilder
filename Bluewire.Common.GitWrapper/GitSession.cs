using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Common.GitWrapper.Parsing;
using CliWrap.Buffered;

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

        public async Task<GitWorkingCopy> FindWorkingCopyContaining(string path)
        {
            if (!Path.IsPathRooted(path)) throw new ArgumentException($"Not an absolute path: {path}");
            if (File.Exists(path)) path = Path.GetDirectoryName(path);
            if (!Directory.Exists(path)) throw new ArgumentException($"Directory does not exist: {path}");

            var command = CommandHelper.CreateCommand("rev-parse", "--show-toplevel");

            var result = await command
                .WithWorkingDirectory(path)
                .LogMinorInvocation(logger, out var log)
                .ExecuteBufferedAsync()
                .LogResult(log);

            if (result.ExitCode != 0) return null;
            var rootPath = GitHelpers.ExpectOneLine(command, result);
            if (String.IsNullOrEmpty(rootPath)) return null;
            return new GitWorkingCopy(Path.GetFullPath(rootPath));
        }

        public async Task<Ref> GetCurrentBranch(GitWorkingCopy workingCopy)
        {
            var command = CommandHelper.CreateCommand("rev-parse", "--abbrev-ref", "HEAD");

            var result = await command
                .RunFrom(workingCopy)
                .LogMinorInvocation(logger, out var log)
                .ExecuteBufferedAsync()
                .LogResult(log);

            var currentBranchName = GitHelpers.ExpectOneLine(command, result);
            var currentBranch = new Ref(currentBranchName);

            if (!Ref.IsBuiltIn(currentBranch)) return currentBranch;

            return await ResolveRef(workingCopy, currentBranch);
        }

        public async Task<Ref> ResolveRef(IGitFilesystemContext workingCopyOrRepo, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var command = CommandHelper.CreateCommand("rev-list", "--max-count=1", @ref);

            var result = await command
                .RunFrom(workingCopyOrRepo)
                .LogMinorInvocation(logger, out var log)
                .ExecuteBufferedAsync()
                .LogResult(log);

            var refHash = GitHelpers.ExpectOneLine(command, result);
            return new Ref(refHash);
        }

        public async Task<bool> RefExists(IGitFilesystemContext workingCopyOrRepo, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var command = CommandHelper.CreateCommand("rev-parse", "--quiet", "--verify", $"{@ref}^{{commit}}");

            var result = await command
                .RunFrom(workingCopyOrRepo)
                .LogMinorInvocation(logger, out var log)
                .ExecuteAsync()
                .LogResult(log, true);

            return result.ExitCode == 0;
        }

        public async Task<bool> ExactRefExists(IGitFilesystemContext workingCopyOrRepo, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var command = CommandHelper.CreateCommand("show-ref", "--verify", "--quiet", @ref);

            var result = await command
                .RunFrom(workingCopyOrRepo)
                .LogMinorInvocation(logger, out var log)
                .ExecuteAsync()
                .LogResult(log, true);

            return result.ExitCode == 0;
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
            var command = CommandHelper.CreateCommand("status", "--porcelain");

            var stdoutEmpty = true;

            var result = await command
                .RunFrom(workingCopy)
                .CaptureErrors(out var checker)
                .LogMinorInvocation(logger, out var log)
                .TeeStandardOutput(_ => stdoutEmpty = false)
                .ExecuteAsync()
                .LogResult(log);

            checker.CheckSuccess(result);

            return stdoutEmpty;
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

            var command = CommandHelper.CreateCommand("init", repoName);

            var result = await command
                .WithWorkingDirectory(containingPath)
                .CaptureErrors(out var checker)
                .LogInvocation(logger, out var log)
                .ExecuteAsync()
                .LogResult(log);

            checker.CheckSuccess(result);

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

            var command = CommandHelper.CreateCommand("clone", remote.ToString(), repoName);

            var result = await command
                .WithWorkingDirectory(containingPath)
                .CaptureErrors(out var checker)
                .LogInvocation(logger, out var log)
                .ExecuteAsync()
                .LogResult(log);

            checker.CheckSuccess(result);

            var workingCopy = new GitWorkingCopy(Path.Combine(containingPath, repoName));
            workingCopy.CheckExistence();
            return workingCopy;
        }

        /// <summary>
        /// Fetch into the repository from a named remote.
        /// </summary>
        public async Task Fetch(IGitFilesystemContext workingCopyOrRepo, string remoteName = null)
        {
            var command = CommandHelper.CreateCommand("fetch", remoteName);
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, command);
        }

        public async Task AddFile(GitWorkingCopy workingCopy, string relativePath)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));
            if (!File.Exists(workingCopy.Path(relativePath))) throw new FileNotFoundException($"File does not exist: {relativePath}", workingCopy.Path(relativePath));

            var command = CommandHelper.CreateCommand("add", relativePath);
            await CommandHelper.RunSimpleCommand(workingCopy, command);
        }

        public async Task<GitStatusEntry[]> Status(GitWorkingCopy workingCopy)
        {
            if (workingCopy == null) throw new ArgumentNullException(nameof(workingCopy));

            var parser = new GitStatusParser();
            var command = CommandHelper.CreateCommand("status", "--porcelain");
            return await CommandHelper.RunCommand(workingCopy, command, parser);
        }

        public async Task<Ref[]> ListBranches(IGitFilesystemContext workingCopyOrRepo, ListBranchesOptions options = default(ListBranchesOptions))
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var command = CommandHelper.CreateCommand("branch", "--list")
                .AddArguments(args =>
                {
                    if (options.Remote) args.Add("--remotes");
                    if (options.UnmergedWith != null) args.Add("--no-merged", options.UnmergedWith);
                    if (options.MergedWith != null) args.Add("--merged", options.MergedWith);
                    if (options.Contains != null) args.Add("--contains", options.Contains);
                    if (options.BranchFilter != null) args.Add(options.BranchFilter);
                });

            var parser = new GitListBranchesParser();
            return await CommandHelper.RunCommand(workingCopyOrRepo, command, parser);
        }

        class GitListBranchesParser : GitLineOutputParser<Ref>
        {
            public override bool Parse(string line, out Ref entry)
            {
                entry = null;
                if (string.IsNullOrWhiteSpace(line)) return false;

                var name = line.Substring(2);
                if (name.StartsWith("(detach")) return false;
                if (name.Contains(" -> ")) return false;
                if (name.StartsWith("(HEAD detached ")) return false;

                entry = new Ref(name);
                return true;
            }
        }

        public async Task<Ref> CreateBranch(IGitFilesystemContext workingCopyOrRepo, string branchName, Ref start = null)
        {
            var branch = new Ref(branchName);
            var command = CommandHelper.CreateCommand("branch", branch, start ?? Ref.Head);
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, command);
            return branch;
        }

        public async Task<Ref[]> ListTags(IGitFilesystemContext workingCopyOrRepo, ListTagsOptions options = default(ListTagsOptions))
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var command = CommandHelper.CreateCommand("tag", "--list")
                .AddArguments(args =>
                {
                    if (options.Contains != null) args.Add("--contains", options.Contains);
                    if (!String.IsNullOrWhiteSpace(options.Pattern)) args.Add(options.Pattern);
                });

            var parser = new GitListTagsParser();
            return await CommandHelper.RunCommand(workingCopyOrRepo, command, parser);
        }

        class GitListTagsParser : GitLineOutputParser<Ref>
        {
            public override bool Parse(string line, out Ref entry)
            {
                entry = null;
                if (string.IsNullOrWhiteSpace(line)) return false;
                entry = new Ref(line);
                return true;
            }
        }

        public async Task<Ref> CreateTag(IGitFilesystemContext workingCopyOrRepo, string tagName, Ref tagLocation, string message, bool force = false)
        {
            if (tagLocation == null) throw new ArgumentNullException(nameof(tagLocation));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var tag = new Ref(tagName);
            var command = CommandHelper.CreateCommand("tag", tag)
                .AddArguments(args =>
                {
                    if (force) args.Add("--force");
                    args.Add("--message", message);
                    args.Add(tagLocation);
                });

            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, command);

            return tag;
        }

        public async Task<Ref> CreateAnnotatedTag(IGitFilesystemContext workingCopyOrRepo, string tagName, Ref tagLocation, string message, bool force = false)
        {
            if (tagLocation == null) throw new ArgumentNullException(nameof(tagLocation));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var tag = new Ref(tagName);
            var command = CommandHelper.CreateCommand("tag", "--annotate", tag)
                .AddArguments(args =>
                {
                    if (force) args.Add("--force");
                    args.Add("--message", message);
                    args.Add(tagLocation);
                });

            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, command);

            return tag;
        }

        public async Task DeleteTag(IGitFilesystemContext workingCopyOrRepo, Ref tag)
        {
            var command = CommandHelper.CreateCommand("tag", "--delete", tag);
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, command);
        }

        public async Task<TagDetails> ReadTagDetails(IGitFilesystemContext workingCopyOrRepo, Ref tag)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));

            var parser = new GitTagDetailsParser();
            var command = CommandHelper.CreateCommand("cat-file", "tag", tag);

            // Not expecting large amounts of data, so just buffer it all:
            var lines = await CommandHelper.RunCommand(workingCopyOrRepo, command, new GitLineOutputCollector());
            var details = parser.Parse(lines);
            if (parser.Errors.Any())
            {
                throw new UnexpectedGitOutputFormatException(command, parser.Errors.ToArray());
            }
            return details;
        }

        public async Task<Ref> CreateBranchAndCheckout(GitWorkingCopy workingCopy, string branchName, Ref start = null)
        {
            var branch = new Ref(branchName);
            var command = CommandHelper.CreateCommand("checkout", "-b", branch, start ?? Ref.Head);
            await CommandHelper.RunSimpleCommand(workingCopy, command);
            return branch;
        }

        public async Task DeleteBranch(IGitFilesystemContext workingCopyOrRepo, Ref branch, bool force = false)
        {
            // I'm currently running Git v1.9.5, which doesn't understand 'branch --delete --force'
            var command = CommandHelper.CreateCommand("branch", force ? "-D" : "--delete", branch);
            await CommandHelper.RunSimpleCommand(workingCopyOrRepo, command);
        }

        public async Task Commit(GitWorkingCopy workingCopy, string message, CommitOptions options = 0)
        {
            var command = CommandHelper.CreateCommand("commit").
                AddArguments(args =>
                {
                    if (options.HasFlag(CommitOptions.AllowEmptyCommit)) args.Add("--allow-empty");
                    args.Add("--message", message);
                });
            await CommandHelper.RunSimpleCommand(workingCopy, command);
        }

        public async Task Checkout(GitWorkingCopy workingCopy, Ref @ref)
        {
            var command = CommandHelper.CreateCommand("checkout", @ref);
            await CommandHelper.RunSimpleCommand(workingCopy, command);
        }

        public async Task CheckoutCompletelyClean(GitWorkingCopy workingCopy, Ref @ref = null)
        {
            var checkoutCmd = CommandHelper.CreateCommand("checkout", "--force", @ref);
            await CommandHelper.RunSimpleCommand(workingCopy, checkoutCmd);
            var cleanCmd = CommandHelper.CreateCommand("clean", "--force", "-xd");
            await CommandHelper.RunSimpleCommand(workingCopy, cleanCmd);
        }

        public async Task ResetCompletelyClean(GitWorkingCopy workingCopy, Ref @ref = null)
        {
            var resetCmd = CommandHelper.CreateCommand("reset", "--hard", @ref);
            await CommandHelper.RunSimpleCommand(workingCopy, resetCmd);
            var cleanCmd = CommandHelper.CreateCommand("clean", "--force", "-xd");
            await CommandHelper.RunSimpleCommand(workingCopy, cleanCmd);
        }

        // TODO: Better API for 'git reset'
        public async Task Reset(GitWorkingCopy workingCopy, ResetType how, Ref @ref)
        {
            var option = "--" + how.ToString().ToLower();

            var command = CommandHelper.CreateCommand("reset", option, @ref);
            await CommandHelper.RunSimpleCommand(workingCopy, command);
        }

        public Task Merge(GitWorkingCopy workingCopy, params Ref[] @refs)
        {
            return Merge(workingCopy, default(MergeOptions), @refs);
        }

        public async Task Merge(GitWorkingCopy workingCopy, MergeOptions options, params Ref[] @refs)
        {
            var command = CommandHelper.CreateCommand("merge")
                .AddArguments(args =>
                {
                    if (options.FastForward == MergeFastForward.Never)
                    {
                        args.Add("--no-ff");
                    }
                    else if (options.FastForward == MergeFastForward.Only)
                    {
                        args.Add("--ff-only");
                    }
                    args.Add(refs.Select(r => r.ToString()));
                });

            await CommandHelper.RunSimpleCommand(workingCopy, command);
        }

        public async Task AbortMerge(GitWorkingCopy workingCopy)
        {
            var command = CommandHelper.CreateCommand("merge", "--abort");
            await CommandHelper.RunSimpleCommand(workingCopy, command);
        }

        public async Task<Ref> MergeBase(IGitFilesystemContext workingCopy, Ref mergeTarget, params Ref[] mergeSources)
        {
            var command = CommandHelper.CreateCommand("merge-base")
                .AddArguments(args =>
                {
                    args.Add(mergeTarget);
                    args.Add(mergeSources.Select(r => r.ToString()));
                });

            var mergeBase = await CommandHelper.RunSingleLineCommand(workingCopy, command);

            return String.IsNullOrWhiteSpace(mergeBase) ? null : new Ref(mergeBase);
        }

        public async Task<bool> IsAncestor(IGitFilesystemContext workingCopy, Ref maybeAncestor, Ref reference)
        {
            var command = CommandHelper.CreateCommand("merge-base", "--is-ancestor", maybeAncestor, reference);
            return await CommandHelper.RunTestCommand(workingCopy, command);
        }

        public async Task<bool> IsFirstParentAncestor(IGitFilesystemContext workingCopyOrRepo, Ref maybeFirstParentAncestor, Ref reference)
        {
            var command = CommandHelper.CreateCommand("rev-list", "--first-parent", reference, "--not", $"{maybeFirstParentAncestor}^@", "--");
            var commits = await CommandHelper.RunCommand(workingCopyOrRepo, command, new GitListCommitsParser());
            return commits.Contains(maybeFirstParentAncestor);
        }

        /// <summary>
        /// List revisions on the first-parent ancestry chain between 'start' and 'end'.
        /// </summary>
        /// <remarks>
        /// * The revisions are listed in reverse order (most recent first) so the first entry will be the resolved SHA1 of 'end'.
        /// * The 'start' revision is not included.
        /// </remarks>
        public async Task<Ref[]> ListCommitsBetween(IGitFilesystemContext workingCopyOrRepo, Ref start, Ref end, ListCommitsOptions options = default(ListCommitsOptions))
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (end == null) throw new ArgumentNullException(nameof(end));

            var command = CommandHelper.CreateCommand("rev-list", new Difference(start, end))
                .AddArguments(args =>
                {
                    if (options.FirstParentOnly) args.Add("--first-parent");
                    if (options.AncestryPathOnly) args.Add("--ancestry-path");
                });
            return await CommandHelper.RunCommand(workingCopyOrRepo, command, new GitListCommitsParser());
        }

        class GitListCommitsParser : GitLineOutputParser<Ref>
        {
            public override bool Parse(string line, out Ref entry)
            {
                entry = new Ref(line);
                return true;
            }
        }

        public async Task<LogEntry[]> ReadLog(IGitFilesystemContext workingCopyOrRepo, LogOptions options, params IRefRange[] refRanges)
        {
            var parser = new GitLogParser();
            var command = CommandHelper.CreateCommand("log")
                .AddArguments(args =>
                {
                    if (options.MatchMessage != null) args.Add("--grep", options.MatchMessage.ToString());
                    if (options.ShowMerges == LogShowMerges.Never)
                    {
                        args.Add("--no-merges");
                    }
                    else if (options.ShowMerges == LogShowMerges.Only)
                    {
                        args.Add("--merges");
                    }

                    if (options.AncestryPathOnly) args.Add("--ancestry-path");

                    if (options.IncludeAllRefs)
                    {
                        if (refRanges.Length > 0) throw new ArgumentException($"IncludeAllRefs was specified, but so were {refRanges.Length} ref ranges.");
                        args.Add("--all");
                    }
                    else
                    {
                        args.Add(refRanges.Select(r => r.ToString()));
                    }
                });

            return await CommandHelper.RunCommand(workingCopyOrRepo, command, parser);
        }

        public async Task<ISet<Ref>> AddAncestry(IGitFilesystemContext workingCopyOrRepo, CommitGraph graph, IRefRange range)
        {
            var command = CommandHelper.CreateCommand("rev-list", "--parents", range.ToString());

            var newRefs = new HashSet<Ref>();
            await CommandHelper.RunCommand(workingCopyOrRepo, command, line =>
            {
                var shas = line.Split(' ');
                var commit = shas.First();
                if (string.IsNullOrWhiteSpace(commit)) return;
                var parents = shas.Skip(1).Select(s => new Ref(s)).ToArray();
                if (graph.Add(new Ref(commit), parents))
                {
                    newRefs.Add(new Ref(commit));
                }
            });
            return newRefs;
        }

        /// <summary>
        /// List paths which exist within the specified tree-ish.
        /// </summary>
        public async Task<PathItem[]> ListPaths(IGitFilesystemContext workingCopyOrRepo, Ref treeish, ListPathsOptions options = default(ListPathsOptions))
        {
            if (treeish == null) throw new ArgumentNullException(nameof(treeish));

            var command = CommandHelper.CreateCommand("ls-tree")
                .AddArguments(args =>
                {
                    switch (options.Mode)
                    {
                        case ListPathsOptions.ListPathsMode.OneLevel:
                            break;
                        case ListPathsOptions.ListPathsMode.Recursive:
                            args.Add("-r");
                            args.Add("-t");
                            break;
                        case ListPathsOptions.ListPathsMode.RecursiveFilesOnly:
                            args.Add("-r");
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    args.Add("--format=%(objecttype) %(objectname) %(path)");
                    args.Add(treeish);
                });
            return await CommandHelper.RunCommand(workingCopyOrRepo, command, new GitListPathsParser(options));
        }

        class GitListPathsParser : GitLineOutputParser<PathItem>
        {
            private readonly ListPathsOptions options;

            public GitListPathsParser(ListPathsOptions options)
            {
                this.options = options;
            }

            public override bool Parse(string line, out PathItem entry)
            {
                entry = default;
                var parts = line.Split(new [] { ' ' }, 3);
                if (!new ObjectTypeParser().TryParse(parts.ElementAtOrDefault(0), out var type)) return false;

                var name = parts.ElementAtOrDefault(1);
                if (string.IsNullOrWhiteSpace(name)) return false;

                var path = parts.ElementAtOrDefault(2);
                if (string.IsNullOrWhiteSpace(path)) return false;

                if (options.PathFilter != null)
                {
                    if (!options.PathFilter(path)) return false;
                }
                entry = new PathItem
                {
                    ObjectType = type,
                    ObjectName = new Ref(name),
                    Path = path,
                };
                return true;
            }
        }

        /// <summary>
        /// Get the content of the specified blob and write it to a stream.
        /// </summary>
        public async Task ReadBlob(IGitFilesystemContext workingCopyOrRepo, Ref objectName, Stream targetStream)
        {
            if (objectName == null) throw new ArgumentNullException(nameof(objectName));

            var command = CommandHelper.CreateCommand("cat-file")
                .AddArguments(args =>
                {
                    args.Add("-p");
                    args.Add(objectName);
                });
            await CommandHelper.RunStreamOutputCommand(workingCopyOrRepo, command, targetStream);
        }
    }
}
