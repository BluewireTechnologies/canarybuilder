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
        private readonly IGitFilesystemContext workingCopyOrRepo;

        public TargetBranchResolver(GitSession gitSession, IGitFilesystemContext workingCopyOrRepo)
        {
            if (gitSession == null) throw new ArgumentNullException(nameof(gitSession));
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));
            this.gitSession = gitSession;
            this.workingCopyOrRepo = workingCopyOrRepo;
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

            return await inspector.FindContainingBranches(workingCopyOrRepo, types, hash, 10); // Tolerate some duplicate integration points for performance reasons.
        }
    }
}
