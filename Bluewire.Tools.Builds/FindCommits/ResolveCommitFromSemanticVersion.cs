using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.GitRepository;
using System.Collections.Generic;
using Bluewire.Tools.Builds.Shared;
using System;

namespace Bluewire.Tools.Builds.FindCommits
{
    public class ResolveCommitFromSemanticVersion : IBuildVersionResolutionJob
    {
        private readonly SemanticVersion semVer;

        public ResolveCommitFromSemanticVersion(string semVer)
        {
            this.semVer = SemanticVersion.FromString(semVer);
        }
        
        public async Task<Build[]> ResolveCommits(GitSession session, Common.GitWrapper.GitRepository repository)
        {

            // We expect only BuildNumberOutOfRangeException and BuildNumberNotFoundException exceptions.
            // The caller of this method could notify the user that no builds were found but doesn't need the specifics.

            if (semVer.IsComplete)
            {
                try
                {
                    var commit = await FindRemoteCommit(session, repository, semVer);
                    if (commit != null)
                    {
                        return new Build[1] { new Build() { Commit = commit, SemanticVersion = semVer } };
                    }
                }
                catch (BuildNumberOutOfRangeException) { }
                catch (BuildNumberNotFoundException) { }
                return new Build[0];
            }

            var refs = new List<Build>(4);

            foreach (string semTag in SemanticVersion.KnownSemanticTags)
            {
                var implicitSemVer = new SemanticVersion(semVer.Major, semVer.Minor, semVer.Build, semTag);
                try
                {
                    var commit = await FindRemoteCommit(session, repository, implicitSemVer);
                    if (commit != null)
                    {
                        refs.Add(new Build() { Commit = commit, SemanticVersion = implicitSemVer });
                    }
                }
                catch (BuildNumberOutOfRangeException) { }
                catch (BuildNumberNotFoundException) { }
            }

            if (refs.Count == 0) return new Build[0];

            return BuildUtils.DeduplicateAndPrioritiseResult(refs.ToArray());
        }

        private async Task<Ref> FindRemoteCommit(GitSession session, IGitFilesystemContext repository, SemanticVersion semVer)
        {
            var resolver = new TopologicalBuildNumberResolver(session);
            var branchSemantics = new BranchSemantics();

            var startBranchName = new Ref(branchSemantics.GetVersionZeroBranchName(semVer));
            var endLocalBranchName = branchSemantics.GetVersionLatestBranchName(semVer);
            if (string.IsNullOrEmpty(endLocalBranchName) || string.IsNullOrEmpty(startBranchName)) return null;

            var startRef = new Ref(startBranchName);
            var endRef = await GetEndRef(session, repository, semVer, endLocalBranchName);

            // It's less likely that the end ref will exist
            if (!await session.RefExists(repository, endRef)) return null;
            if (!await session.RefExists(repository, startRef)) return null;

            return await resolver.FindCommit(repository, startRef, endRef, semVer.Build);
        }

        private static async Task<Ref> GetEndRef(GitSession session, IGitFilesystemContext repository, SemanticVersion semVer, string endLocalBranchName)
        {
            if (endLocalBranchName == "master")
            {
                var maintTag = new Ref($"tags/maint/{semVer.Major}.{semVer.Minor}");
                if (await session.TagExists(repository, maintTag))
                {
                    return RefHelper.GetRemoteRef(new Ref(maintTag));
                }
            }
            return RefHelper.GetRemoteRef(new Ref(endLocalBranchName));
        }
    }
}
