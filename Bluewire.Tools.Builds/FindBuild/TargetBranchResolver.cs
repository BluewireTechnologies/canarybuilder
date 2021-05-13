using System;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.Builds.Shared;
using Bluewire.Tools.GitRepository;

namespace Bluewire.Tools.Builds.FindBuild
{
    public class TargetBranchResolver
    {
        private readonly GitSession gitSession;
        private readonly Common.GitWrapper.GitRepository repository;

        public TargetBranchResolver(GitSession gitSession, Common.GitWrapper.GitRepository repository)
        {
            if (gitSession == null) throw new ArgumentNullException(nameof(gitSession));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            this.gitSession = gitSession;
            this.repository = repository;
        }

        public async Task<StructuredBranch[]> IdentifyTargetBranchesOfCommit(Ref hash)
        {
            var inspector = new RepositoryStructureInspector(gitSession);

            var types = new[]
            {
                BranchType.Beta,
                BranchType.ReleaseCandidate,
                BranchType.Release
            };

            return await inspector.FindContainingBranches(repository, types, hash, 10); // Tolerate some duplicate integration points for performance reasons.
        }
    }
}
