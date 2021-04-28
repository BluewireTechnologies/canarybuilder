using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using RefCleaner.Collectors;

namespace RefCleaner
{
    public class RefCollectorFactory
    {
        private readonly string repositoryPath;
        private readonly string remoteName;

        public RefCollectorFactory(string repositoryPath, string remoteName, IConsoleInvocationLogger logger = null)
        {
            this.repositoryPath = repositoryPath;
            this.remoteName = remoteName;
            sharedGitSession = new Lazy<Task<GitSession>>(async () => new GitSession(await new GitFinder().FromEnvironment(), logger));
        }

        private GitRepository FindAndVerifyRepository()
        {
            try
            {
                return GitRepository.Find(repositoryPath);
            }
            catch (Exception ex)
            {
                throw new InvalidArgumentsException(ex);
            }
        }

        public async Task<IRefCollector> CreateExpiredCanaryTagCollector()
        {
            var gitRepository = FindAndVerifyRepository();

            return new DatestampedTagCollector(await sharedGitSession.Value, gitRepository, remoteName, "canary/archive-*", 7);
        }

        public async Task<IRefCollector> CreateBranchCollectors()
        {
            var gitRepository = FindAndVerifyRepository();

            var branchProvider = new CachingBranchProvider(await GetBranchProvider(gitRepository));

            var filters = GetBranchFilters(branchProvider).ToArray();
            return new FilteredBranchCollector(branchProvider, filters);
        }

        private async Task<IBranchProvider> GetBranchProvider(IGitFilesystemContext gitRepository)
        {
            var session = await sharedGitSession.Value;
            if (String.IsNullOrWhiteSpace(remoteName)) return new LocalBranchProvider(session, gitRepository);
            return new RemoteBranchProvider(session, gitRepository, remoteName);
        }

        private IEnumerable<IRefFilter> GetBranchFilters(IBranchProvider branchProvider)
        {
            yield return new KeepAllReleaseBranches();
            yield return new KeepAllPersonalBranches();
            yield return new KeepRecentBranches(DateTimeOffset.Now.AddMonths(-1));
            yield return new DiscardMergedBranches(new MergedBranchTester(branchProvider));
        }

        private readonly Lazy<Task<GitSession>> sharedGitSession;
    }
}
