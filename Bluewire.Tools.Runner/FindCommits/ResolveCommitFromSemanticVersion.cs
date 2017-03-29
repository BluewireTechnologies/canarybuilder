using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.GitRepository;
using System.Collections.Generic;
using Bluewire.Tools.Runner.Shared;
using System.Linq;
using Bluewire.Tools.Runner.FindBuild;

namespace Bluewire.Tools.Runner.FindCommits
{
    public class ResolveCommitsFromSemanticVersion : IBuildVersionResolutionJob
    {
        private readonly SemanticVersion semVer;

        public ResolveCommitsFromSemanticVersion(string semVer)
        {
            this.semVer = SemanticVersion.FromString(semVer);
        }
        
        public async Task<Build[]> ResolveCommits(GitSession session, Common.GitWrapper.GitRepository repository)
        {
            var candidateVersions = semVer.IsComplete ? new [] { semVer } : SemanticVersion.KnownSemanticTags.Select(t => new SemanticVersion(semVer.Major, semVer.Minor, semVer.Build, t));

            var branchSemantics = new BranchSemantics();
            var startTagName = RefHelper.PutInHierarchy("tags", new Ref(branchSemantics.GetVersionZeroBranchName(semVer)));

            var candidateTargetBranches = GetTargetBranches(branchSemantics, candidateVersions);
            var resolvedCommits = await ResolveToCommits(session, repository, startTagName, candidateTargetBranches.ToArray(), semVer.Build);
            var builds = new List<Build>();
            var buildVersionFinder = new BuildVersionFinder(session, repository);
            foreach (var group in resolvedCommits.GroupBy(i => i.Commit, i => i.TargetBranch))
            {
                var branch = GetPreferredBranch(group);
                var actualVersion = await buildVersionFinder.GetBuildNumberForAssumedBranch(group.Key, branch);

                if (actualVersion.Minor != semVer.Minor) continue;
                if (actualVersion.Major != semVer.Major) continue;

                builds.Add(new Build { Commit = group.Key, SemanticVersion = actualVersion });
            }
            return builds.ToArray();
        }

        class BuildNumberResolutionResult
        {
            public Ref Commit { get; set; }
            public StructuredBranch TargetBranch { get; set; }
        }

        private static StructuredBranch GetPreferredBranch(IEnumerable<StructuredBranch> targetBranches)
        {
            return targetBranches.OrderByDescending(new BranchTypeScorer().Score).First();
        }

        private IEnumerable<StructuredBranch> GetTargetBranches(BranchSemantics semantics, IEnumerable<SemanticVersion> versions)
        {
            foreach(var version in versions)
            {
                StructuredBranch branch;
                if (StructuredBranch.TryParse(semantics.GetVersionLatestBranchName(version), out branch)) yield return branch;
            }
        }

        private async Task<List<BuildNumberResolutionResult>> ResolveToCommits(GitSession session, IGitFilesystemContext repository, Ref start, StructuredBranch[] targetBranches, int buildNumber)
        {
            var results = new List<BuildNumberResolutionResult>();
            var resolver = new TopologicalBuildNumberResolver(session);

            var startRef = RefHelper.GetRemoteRef(start);
            if (!await session.RefExists(repository, startRef)) return results;

            foreach (var branch in targetBranches)
            {
                var endRef = RefHelper.GetRemoteRef(new Ref(branch.ToString()));

                // It's less likely that the end ref will exist
                if (!await session.RefExists(repository, endRef)) continue;

                try
                {
                    var commit = await resolver.FindCommit(repository, startRef, endRef, buildNumber);
                    results.Add(new BuildNumberResolutionResult { Commit = commit, TargetBranch = branch });
                }
                catch (BuildNumberOutOfRangeException) { }
                catch (BuildNumberNotFoundException) { }
            }
            return results;
        }
    }
}
