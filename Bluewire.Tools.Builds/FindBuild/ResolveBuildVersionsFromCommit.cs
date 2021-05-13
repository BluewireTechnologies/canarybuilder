using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.Builds.Shared;

namespace Bluewire.Tools.Builds.FindBuild
{
    public class ResolveBuildVersionsFromCommit : IBuildVersionResolutionJob
    {
        private readonly string commitRef;

        public ResolveBuildVersionsFromCommit(string commitRef)
        {
            this.commitRef = commitRef;
        }

        public async Task<SemanticVersion[]> ResolveBuildVersions(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {
            var hash = await ResolveToHash(session, workingCopyOrRepo);

            var resolver = new TargetBranchResolver(session, workingCopyOrRepo);
            var targetBranches = await resolver.IdentifyTargetBranchesOfCommit(hash);

            var finder = new BuildVersionFinder(session, workingCopyOrRepo);
            return await finder.GetBuildVersionsFromCommit(hash, targetBranches);
        }

        private async Task<Ref> ResolveToHash(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {
            try
            {
                return await session.ResolveRef(workingCopyOrRepo, new Ref(commitRef));
            }
            catch (GitException)
            {
                throw new RefNotFoundException(commitRef);
            }
        }
    }
}
