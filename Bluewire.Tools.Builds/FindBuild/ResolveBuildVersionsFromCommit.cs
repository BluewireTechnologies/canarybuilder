﻿using System.Threading.Tasks;
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

        public async Task<SemanticVersion[]> ResolveBuildVersions(GitSession session, Common.GitWrapper.GitRepository repository)
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
                throw new RefNotFoundException(commitRef);
            }
        }
    }
}
