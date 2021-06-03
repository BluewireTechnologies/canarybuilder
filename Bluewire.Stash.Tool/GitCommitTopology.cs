using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.GitRepository;

namespace Bluewire.Stash.Tool
{
    public class GitCommitTopology : ICommitTopology
    {
        private readonly GitSession gitSession;
        private readonly IGitFilesystemContext workingCopyOrRepo;
        private readonly TopologicalBuildNumberProvider buildNumberProvider;
        private readonly TopologyCache topology;

        public GitCommitTopology(GitSession gitSession, IGitFilesystemContext workingCopyOrRepo)
        {
            this.gitSession = gitSession ?? throw new ArgumentNullException(nameof(gitSession));
            this.workingCopyOrRepo = workingCopyOrRepo ?? throw new ArgumentNullException(nameof(workingCopyOrRepo));
            topology = new TopologyCache(gitSession, workingCopyOrRepo);
            buildNumberProvider = new TopologicalBuildNumberProvider(topology);
        }

        public async Task<ResolvedVersionMarker?> FullyResolve(VersionMarker marker)
        {
            if (marker.IsComplete) return marker.Checked;
            var hash = await GetHashRefForVersionMarker(marker);
            if (hash == null) return null;
            var semanticVersion = marker.SemanticVersion ?? await ResolveSemanticVersionFromCommitHash(hash);
            if (semanticVersion == null) return null;
            return new ResolvedVersionMarker(semanticVersion, hash.ToString());
        }

        private async Task<SemanticVersion?> ResolveSemanticVersionFromCommitHash(Ref hash)
        {
            var inspector = new RepositoryStructureInspector(gitSession);

            var versionBaseTag = await inspector.ResolveBaseTagForCommit(workingCopyOrRepo, hash);
            if (versionBaseTag == null) return null;

            return await ResolveSemanticVersionFromCommitHash(hash, versionBaseTag);
        }

        private async Task<SemanticVersion?> ResolveSemanticVersionFromCommitHash(Ref hash, TagDetails versionBaseTag)
        {
            var buildNumber = await buildNumberProvider.GetBuildNumber(versionBaseTag.Ref, hash);
            if (buildNumber == null) return null;

            var prototype = new SemanticVersion(versionBaseTag.Name, buildNumber.Value, BranchType.None);
            var possibleVersions = new []
            {
                prototype.WithTag(BranchType.Release.SemanticTag),
                prototype.WithTag(BranchType.ReleaseCandidate.SemanticTag),
                prototype.WithTag(BranchType.Beta.SemanticTag),
            };
            foreach (var probe in possibleVersions)
            {
                var branchTip = await GetPreferredBranchName(probe);
                if (branchTip == null) continue;
                if (await buildNumberProvider.IsFirstParentAncestor(versionBaseTag.Ref, hash, branchTip)) return probe;
            }
            return prototype.WithTag(new AlphaTagFormatter().Format(hash));
        }

        private async Task<Ref?> GetPreferredBranchName(SemanticVersion probe)
        {
            foreach (var branch in new BranchSemantics().GetVersionLatestBranchNames(probe))
            {
                // Use the first branch which exists. No point exploring master if backport/xx.yy exists.
                if (await topology.BranchExists(new Ref(branch))) return new Ref(branch);
            }
            return null;
        }

        private async Task<string?> ResolveCommitHashFromSemanticVersion(SemanticVersion semanticVersion)
        {
            var startBranchName = new Ref(new BranchSemantics().GetVersionZeroBranchName(semanticVersion));
            if (string.IsNullOrEmpty(startBranchName)) return null;

            var startRef = new Ref(startBranchName);
            var endRef = await new RepositoryStructureInspector(gitSession).ResolveTagOrTipOfBranchForVersion(workingCopyOrRepo, semanticVersion);

            // It's less likely that the end ref will exist
            if (endRef == null) return null;
            if (!await topology.RefExists(startRef)) return null;

            return await buildNumberProvider.FindCommit(startRef, endRef, semanticVersion.Build);
        }

        public async Task<bool> IsAncestor(ResolvedVersionMarker reference, ResolvedVersionMarker subject)
        {
            return await gitSession.IsAncestor(workingCopyOrRepo, new Ref(subject.CommitHash), new Ref(reference.CommitHash));
        }

        public async IAsyncEnumerable<ResolvedVersionMarker> EnumerateAncestry(VersionMarker marker)
        {
            var hash = await GetHashRefForVersionMarker(marker);
            if (hash == null) yield break;

            var versionBaseTag = await new RepositoryStructureInspector(gitSession).ResolveBaseTagForCommit(workingCopyOrRepo, hash);
            if (versionBaseTag == null) yield break;

            var graph = await topology.LoadAncestryPaths(versionBaseTag.ResolvedRef, hash);

            var commits = graph.FirstParentAncestry(hash, versionBaseTag.ResolvedRef);
            foreach (var commit in new [] { hash }.Concat(commits))
            {
                var semanticVersion = await ResolveSemanticVersionFromCommitHash(commit, versionBaseTag);
                if (semanticVersion == null) continue;
                yield return new ResolvedVersionMarker(semanticVersion, commit.ToString());
            }
        }

        private async Task<Ref?> GetHashRefForVersionMarker(VersionMarker marker)
        {
            if (marker.IsValid)
            {
                if (marker.CommitHash != null)
                {
                    var subjectRef = new Ref(marker.CommitHash);
                    if (!await topology.RefExists(subjectRef)) return null;
                    return await topology.ResolveRef(subjectRef);
                }
                if (marker.SemanticVersion != null)
                {
                    var commitHash = await ResolveCommitHashFromSemanticVersion(marker.SemanticVersion);
                    if (commitHash == null) return null;
                    return new Ref(commitHash);
                }
            }
            throw new ArgumentException("Empty VersionMarker", nameof(marker));
        }

        public async Task<ResolvedVersionMarker?> GetLastVersionInMajorMinor(SemanticVersion semVer)
        {
            var possibleVersions = new []
            {
                semVer.WithTag(BranchType.Beta.SemanticTag),
                semVer.WithTag(BranchType.ReleaseCandidate.SemanticTag),
                semVer.WithTag(BranchType.Release.SemanticTag),
            };
            var startRef = new Ref(new BranchSemantics().GetVersionZeroBranchName(semVer));
            foreach (var probe in possibleVersions)
            {
                var branchTip = await GetPreferredBranchName(probe);
                if (branchTip == null) continue;
                var commonAncestor = await gitSession.MergeBase(workingCopyOrRepo, new Ref("master"), branchTip);
                if (commonAncestor == null) continue;

                var buildNumber = await buildNumberProvider.GetBuildNumber(startRef, commonAncestor);
                if (buildNumber == null) continue;
                // As far as we're concerned, the last commit on master for a given version is always a beta.
                return new ResolvedVersionMarker(new SemanticVersion(semVer.Major, semVer.Minor, buildNumber.Value, "beta"), commonAncestor.ToString());
            }
            return null;
        }
    }
}
