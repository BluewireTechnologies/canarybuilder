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
    /// Exposes information about branches, with regard to either a named remote or the local repository.
    /// </summary>
    public class LocalBranchProvider : IBranchProvider
    {
        private readonly GitSession session;
        private readonly IGitFilesystemContext repository;
        private readonly GitCommandHelper helper;

        public LocalBranchProvider(GitSession session, IGitFilesystemContext repository)
        {
            this.session = session;
            this.repository = repository;
            if (session == null) throw new ArgumentNullException(nameof(session));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            helper = session.CommandHelper;
        }

        public async Task<BranchDetails[]> GetAllBranches()
        {
            var command = CreateAllBranchesForEachRefCommand();

            var parser = new BranchDetailsParser();
            var process = repository.Invoke(command);
            var branches = await helper.ParseOutput(process, parser);
            return branches.Where(r => !Ref.IsBuiltIn(r.Ref)).ToArray();
        }

        private CommandLine CreateAllBranchesForEachRefCommand()
        {
            return new CommandLine(helper.Git.GetExecutableFilePath(), "for-each-ref")
                .Add("--format", "%(committerdate:iso-strict) %(objectname) %(refname:strip=2)")
                .Add("refs/heads/");
        }

        public async Task<ICollection<Ref>> GetMergedBranches(Ref mergeTarget)
        {
            var command = CreateMergedBranchesForEachRefCommand(mergeTarget);

            var parser = new RefNameColumnLineParser(0);
            var process = repository.Invoke(command);
            var branches = await helper.ParseOutput(process, parser);
            return branches.Where(r => !Ref.IsBuiltIn(r)).ToArray();
        }
        
        public Task<bool> BranchExists(Ref branch)
        {
            return session.BranchExists(repository, branch);
        }

        private CommandLine CreateMergedBranchesForEachRefCommand(Ref mergeTarget)
        {
            return new CommandLine(helper.Git.GetExecutableFilePath(), "for-each-ref")
                .Add("--merged", mergeTarget)
                .Add("--format", "%(refname:strip=2)")
                .Add("refs/heads/");
        }
    }
}
