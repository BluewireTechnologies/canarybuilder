using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper;
using CanaryCollector.Collectors;
using CanaryCollector.Remote.YouTrack;
using YouTrackSharp.Infrastructure;

namespace CanaryCollector
{
    public class BranchCollectorFactory
    {
        private readonly Uri youtrackUri;
        private readonly string repositoryPath;

        public BranchCollectorFactory(Uri youtrackUri, string repositoryPath, IConsoleInvocationLogger logger = null)
        {
            this.youtrackUri = youtrackUri;
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

        private Connection CreateYoutrackConnection(string dependentParameter)
        {
            if (youtrackUri == null) throw new InvalidArgumentsException($"The --youtrack parameter must be specified in order to use {dependentParameter}.");
            try
            {
                return YouTrack.OpenConnection(youtrackUri);
            }
            catch (Exception ex)
            {
                throw new InvalidArgumentsException(ex);
            }
        }

        public async Task<IBranchCollector> CreatePendingCollector()
        {
            var youtrackConnection = CreateYoutrackConnection("--pending");
            var gitRepository = FindAndVerifyRepository("--pending");

            return new PendingReviewTicketBranchCollector(new YouTrackTicketProvider(youtrackConnection), new GitRemoteBranchProvider(await sharedGitSession.Value, gitRepository));
        }

        public async Task<IBranchCollector[]> CreateTagCollectors(ICollection<string> tags)
        {
            if (!tags.Any()) return new IBranchCollector[0];
            var youtrackConnection = CreateYoutrackConnection("--tag");
            var gitRepository = FindAndVerifyRepository("--tag");
            var branchProvider = new GitRemoteBranchProvider(await sharedGitSession.Value, gitRepository);

            return tags.Select(t => new TaggedTicketBranchCollector(new YouTrackTicketProvider(youtrackConnection), branchProvider, t)).ToArray();
        }

        public IEnumerable<IBranchCollector> CreateUriCollectors(ICollection<Uri> uris)
        {
            if (!uris.Any()) yield break;
            throw new NotImplementedException();
        }

        private readonly Lazy<Task<GitSession>> sharedGitSession;
    }
}
