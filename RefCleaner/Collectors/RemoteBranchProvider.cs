using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Common.GitWrapper.Parsing;

namespace RefCleaner.Collectors
{
    /// <summary>
    /// Exposes information about branches, with regard to a named remote.
    /// </summary>
    public class RemoteBranchProvider : IBranchProvider
    {
        private readonly GitSession session;
        private readonly IGitFilesystemContext repository;
        private readonly string remoteName;
        private readonly GitCommandHelper helper;

        public RemoteBranchProvider(GitSession session, IGitFilesystemContext repository, string remoteName)
        {
            this.session = session;
            this.repository = repository;
            this.remoteName = remoteName;
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            helper = session.CommandHelper;
        }

        public async Task<BranchDetails[]> GetAllBranches()
        {
            var command = CreateAllBranchesForEachRefCommand();

            var parser = new BranchDetailsParser();
            var branches = await helper.RunCommand(repository, command, parser);
            return branches.Where(r => !Ref.IsBuiltIn(r.Ref)).ToArray();
        }

        private CommandLine CreateAllBranchesForEachRefCommand()
        {
            return helper.CreateCommand("for-each-ref")
                .Add("--format", "%(committerdate:iso-strict) %(objectname) %(refname:strip=3)")
                .Add($"refs/remotes/{remoteName}/");
        }

        public async Task<ICollection<Ref>> GetMergedBranches(Ref mergeTarget)
        {
            var command = CreateMergedBranchesForEachRefCommand(mergeTarget);

            var parser = new RefNameColumnLineParser(0);
            var branches = await helper.RunCommand(repository, command, parser);
            return branches.Where(r => !Ref.IsBuiltIn(r)).ToArray();
        }

        public Task<bool> BranchExists(Ref branch)
        {
            return session.ExactRefExists(repository, AsRemoteRef(branch));
        }

        private CommandLine CreateMergedBranchesForEachRefCommand(Ref mergeTarget)
        {
            return helper.CreateCommand("for-each-ref")
                .Add("--merged", AsRemoteRef(mergeTarget))
                .Add("--format", "%(refname:strip=3)")
                .Add($"refs/remotes/{remoteName}/");
        }

        private Ref AsRemoteRef(Ref @ref)
        {
            return RefHelper.PutInHierarchy($"remotes/{remoteName}", @ref);
        }
    }
}
