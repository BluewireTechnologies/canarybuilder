using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.Runner.FindBuild
{
    public class ResolveBuildVersionsFromCommit : IBuildVersionResolutionJob
    {
        private readonly string commitRef;

        public ResolveBuildVersionsFromCommit(string commitRef)
        {
            this.commitRef = commitRef;
        }

        public async Task<string[]> ResolveBuildVersions(GitSession session, Common.GitWrapper.GitRepository repository)
        {
            var hash = await ResolveToHash(session, repository);

            var resolver = new TargetBranchResolver(session, repository);
            var targetBranches = await resolver.IdentifyTargetBranchesOfCommit(hash);

            var finder = new BuildVersionFinder(session, repository);
            return await finder.GetBuildVersionsFromCommit(hash, targetBranches);
        }

        private async Task<Ref> ResolveToHash(GitSession session, Common.GitWrapper.GitRepository repository)
        {
            try
            {
                return await session.ResolveRef(repository, new Ref(commitRef));
            }
            catch (GitException)
            {
                throw new ErrorWithReturnCodeException(3, $"Cannot find the specified ref {commitRef}.");
            }
        }
    }
}
