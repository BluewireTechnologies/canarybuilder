using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper;
using CanaryCollector.Collectors;

namespace CanaryCollector
{
    public class BranchCollectorFactory
    {
        private readonly ITicketProviderFactory ticketProviderFactory;
        private readonly string repositoryPath;

        public BranchCollectorFactory(ITicketProviderFactory ticketProviderFactory, string repositoryPath, IConsoleInvocationLogger logger = null)
        {
            this.ticketProviderFactory = ticketProviderFactory;
            this.repositoryPath = repositoryPath;
            sharedGitSession = new Lazy<Task<GitSession>>(async () => new GitSession(await new GitFinder().FromEnvironment(), logger));
        }

        private GitRepository FindAndVerifyRepository(string dependentParameter)
        {
            if (String.IsNullOrEmpty(repositoryPath)) throw new InvalidArgumentsException($"The --repository parameter must be specified in order to use {dependentParameter}.");
            try
            {
                return GitRepository.Find(repositoryPath);
            }
            catch (Exception ex)
            {
                throw new InvalidArgumentsException(ex);
            }
        }

        public async Task<IBranchCollector> CreatePendingCollector()
        {
            var ticketProvider = ticketProviderFactory.Create("--pending");
            var gitRepository = FindAndVerifyRepository("--pending");

            return new PendingMergeTicketBranchCollector(ticketProvider, new GitRemoteBranchProvider(await sharedGitSession.Value, gitRepository));
        }

        public async Task<IBranchCollector[]> CreateTagCollectors(ICollection<string> tags)
        {
            if (!tags.Any()) return new IBranchCollector[0];
            var ticketProvider = ticketProviderFactory.Create("--tag");
            var gitRepository = FindAndVerifyRepository("--tag");
            var branchProvider = new GitRemoteBranchProvider(await sharedGitSession.Value, gitRepository);

            return tags.Select(t => new TaggedTicketBranchCollector(ticketProvider, branchProvider, t)).ToArray();
        }

        public IEnumerable<IBranchCollector> CreateUriCollectors(ICollection<Uri> uris)
        {
            if (!uris.Any()) yield break;
            throw new NotImplementedException();
        }

        private readonly Lazy<Task<GitSession>> sharedGitSession;
    }
}
