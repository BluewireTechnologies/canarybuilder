using System;
using System.Collections.Generic;
using System.Linq;
using Bluewire.Common.Console;
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

        public BranchCollectorFactory(Uri youtrackUri, string repositoryPath)
        {
            this.youtrackUri = youtrackUri;
            this.repositoryPath = repositoryPath;
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

        public IBranchCollector CreatePendingCollector()
        {
            var youtrackConnection = CreateYoutrackConnection("--pending");
            var gitRepository = FindAndVerifyRepository("--pending");
            return new PendingReviewTicketBranchCollector(new YouTrackTicketProvider(youtrackConnection), gitRepository);
        }

        public IEnumerable<IBranchCollector> CreateTagCollectors(ICollection<string> tags)
        {
            if (!tags.Any()) yield break;
            var youtrackConnection = CreateYoutrackConnection("--tag");
            throw new NotImplementedException();
        }

        public IEnumerable<IBranchCollector> CreateUriCollectors(ICollection<Uri> uris)
        {
            if (!uris.Any()) yield break;
            throw new NotImplementedException();
        }
    }
}