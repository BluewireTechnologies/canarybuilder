using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.GitRepository;
using System.Collections.Generic;
using Bluewire.Tools.Builds.Shared;

namespace Bluewire.Tools.Builds.FindCommits
{
    public class ResolveCommitFromSemanticVersion : IBuildVersionResolutionJob
    {
        private readonly SemanticVersion semVer;

        public ResolveCommitFromSemanticVersion(string semVer)
        {
            this.semVer = SemanticVersion.FromString(semVer);
        }

        public async Task<Build[]> ResolveCommits(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {

            // We expect only BuildNumberOutOfRangeException and BuildNumberNotFoundException exceptions.
            // The caller of this method could notify the user that no builds were found but doesn't need the specifics.

            if (semVer.IsComplete)
            {
                try
                {
                    var commit = await FindRemoteCommit(session, workingCopyOrRepo, semVer);
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
                    var commit = await FindRemoteCommit(session, workingCopyOrRepo, implicitSemVer);
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

        private async Task<Ref> FindRemoteCommit(GitSession session, IGitFilesystemContext workingCopyOrRepo, SemanticVersion semVer)
        {
            var resolver = new TopologicalBuildNumberResolver(session);
            var branchSemantics = new BranchSemantics();

            var startBranchName = new Ref(branchSemantics.GetVersionZeroBranchName(semVer));
            if (string.IsNullOrEmpty(startBranchName)) return null;

            var startRef = new Ref(startBranchName);
            var endRef = await new RepositoryStructureInspector(session).ResolveTagOrTipOfBranchForVersion(workingCopyOrRepo, semVer);

            // It's less likely that the end ref will exist
            if (endRef == null) return null;
            if (!await session.RefExists(workingCopyOrRepo, startRef)) return null;

            return await resolver.FindCommit(workingCopyOrRepo, startRef, endRef, semVer.Build);
        }
    }
}
