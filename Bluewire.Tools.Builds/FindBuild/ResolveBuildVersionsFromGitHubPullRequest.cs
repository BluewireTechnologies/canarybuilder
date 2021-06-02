using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.Builds.Shared;

namespace Bluewire.Tools.Builds.FindBuild
{
    public class ResolveBuildVersionsFromGitHubPullRequest : IBuildVersionResolutionJob
    {
        private readonly int pullRequestNumber;

        public ResolveBuildVersionsFromGitHubPullRequest(int pullRequestNumber)
        {
            this.pullRequestNumber = pullRequestNumber;
        }

        public async Task<SemanticVersion[]> ResolveBuildVersions(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {
            var hash = await ResolveToSingleCommit(session, workingCopyOrRepo);

            var resolver = new TargetBranchResolver(session, workingCopyOrRepo);
            var targetBranches = await resolver.IdentifyTargetBranchesOfCommit(hash);

            var finder = new BuildVersionFinder(session, workingCopyOrRepo);
            return await finder.GetBuildVersionsFromCommit(hash, targetBranches);
        }

        private async Task<Ref> ResolveToSingleCommit(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {
            var pattern = $"^Merge pull request #{pullRequestNumber}\\b";

            var prMerges = await session.ReadLog(workingCopyOrRepo, new LogOptions { MatchMessage = new Regex(pattern), IncludeAllRefs = true });

            if (prMerges.Length == 1) return prMerges.Single().Ref;
            if (prMerges.Length == 0) throw new PullRequestMergeNotFoundException(pullRequestNumber);

            // The first parent is the commit into which the other commits were merged.
            var mergedCommits = prMerges.Select(m => m.MergeParents?.Skip(1)).Aggregate((rs, r) => r == null ? Enumerable.Empty<Ref>() : rs.Intersect(r)).ToArray();
            if (mergedCommits.Length == 1) return mergedCommits.Single();
            throw new PullRequestMergesHaveNoCommonParentException(pullRequestNumber, prMerges.Select(p => p.Ref).ToArray());
        }
    }
}
