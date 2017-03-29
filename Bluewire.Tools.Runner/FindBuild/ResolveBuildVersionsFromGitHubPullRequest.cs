using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace Bluewire.Tools.Runner.FindBuild
{
    public class ResolveBuildVersionsFromGitHubPullRequest : IBuildVersionResolutionJob
    {
        private readonly int pullRequestNumber;

        public ResolveBuildVersionsFromGitHubPullRequest(int pullRequestNumber)
        {
            this.pullRequestNumber = pullRequestNumber;
        }

        public async Task<SemanticVersion[]> ResolveBuildVersions(GitSession session, Common.GitWrapper.GitRepository repository)
        {
            var hash = await ResolveToSingleCommit(session, repository);

            var resolver = new TargetBranchResolver(session, repository);
            var targetBranches = await resolver.IdentifyTargetBranchesOfCommit(hash);

            var finder = new BuildVersionFinder(session, repository);
            return await finder.GetBuildVersionsFromCommit(hash, targetBranches);
        }

        private async Task<Ref> ResolveToSingleCommit(GitSession session, Common.GitWrapper.GitRepository repository)
        {
            var pattern = $"^Merge pull request #{pullRequestNumber}\\b";

            var prMerges = await session.ReadLog(repository, new LogOptions { MatchMessage = new Regex(pattern), IncludeAllRefs = true });

            if (prMerges.Length == 1) return prMerges.Single().Ref;
            if (prMerges.Length == 0) throw new ErrorWithReturnCodeException(3, $"Could not find a merge of PR #{pullRequestNumber}.");

            // The first parent is the commit into which the other commits were merged.
            var mergedCommits = prMerges.Select(m => m.MergeParents?.Skip(1)).Aggregate((rs, r) => r == null ? Enumerable.Empty<Ref>() : rs.Intersect(r)).ToArray();
            if (mergedCommits.Length == 1) return mergedCommits.Single();
            throw new ErrorWithReturnCodeException(3, $"Could not find a common parent of all {prMerges.Length} merges of PR #{pullRequestNumber}. Please specify a commit instead.");
        }
    }
}
