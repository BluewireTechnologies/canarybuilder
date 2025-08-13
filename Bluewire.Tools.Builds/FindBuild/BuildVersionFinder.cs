using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.GitRepository;
using Bluewire.Tools.Builds.Shared;

namespace Bluewire.Tools.Builds.FindBuild
{
    public class BuildVersionFinder
    {
        private readonly GitSession gitSession;
        private readonly IGitFilesystemContext workingCopyOrRepo;

        public BuildVersionFinder(GitSession gitSession, IGitFilesystemContext workingCopyOrRepo)
        {
            if (gitSession == null) throw new ArgumentNullException(nameof(gitSession));
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));
            this.gitSession = gitSession;
            this.workingCopyOrRepo = workingCopyOrRepo;
        }

        public async Task<SemanticVersion[]> GetBuildVersionsFromCommit(Ref commitHash, StructuredBranch[] targetBranches)
        {
            var integrationPoints = await QueryIntegrationPoints(commitHash, targetBranches);

            var buildNumbers = new List<SemanticVersion>();
            foreach (var group in integrationPoints.GroupBy(i => i.IntegrationPoint, i => i.TargetBranch))
            {
                var branch = GetPreferredBranch(group);
                buildNumbers.Add(await GetBuildNumberFromIntegrationPoint(group.Key, branch));
            }
            return buildNumbers.ToArray();
        }

        public async Task<IntegrationQueryResult[]> QueryIntegrationPoints(Ref subject, StructuredBranch[] targetBranches)
        {
            var inspector = new RepositoryStructureInspector(gitSession);
            var baseTag = await inspector.ResolveBaseTagForCommit(workingCopyOrRepo, subject);

            var integrationPoints = new List<IntegrationQueryResult>();
            var integrationPointLocator = new BranchIntegrationPointLocator(gitSession);
            foreach (var branch in targetBranches)
            {
                integrationPoints.Add(new IntegrationQueryResult {
                    Subject = subject,
                    TargetBranch = branch,
                    IntegrationPoint = await integrationPointLocator.FindCommit(workingCopyOrRepo, baseTag.ResolvedRef, new Ref(branch.ToString()), subject)
                });
            }
            return integrationPoints.ToArray();
        }

        private static StructuredBranch GetPreferredBranch(IEnumerable<StructuredBranch> targetBranches)
        {
            return targetBranches.OrderByDescending(new BranchTypeScorer().Score).First();
        }

        class BranchTypeScorer
        {
            public int Score(StructuredBranch branch)
            {
                var type = new BranchSemantics().GetBranchType(branch);
                if (branch.IsMaster()) return 5;
                if (BranchType.Release.Equals(type)) return 3;
                if (BranchType.ReleaseCandidate.Equals(type)) return 1;
                return 0;
            }
        }

        private async Task<SemanticVersion> GetBuildNumberFromIntegrationPoint(Ref mergeCommit, StructuredBranch branch)
        {
            var inspector = new RepositoryStructureInspector(gitSession);

            var sprintNumber = SprintNumber.Parse(branch.ToString());
            var versionNumber = await inspector.GetActiveVersionNumber(workingCopyOrRepo, mergeCommit);
            var baseTag = await inspector.ResolveBaseTagForVersion(workingCopyOrRepo, versionNumber);

            if (sprintNumber != null) Debug.Assert(sprintNumber == Version.Parse(versionNumber));

            var calculator = new TopologicalBuildNumberProvider(gitSession, workingCopyOrRepo);
            var buildNumber = await calculator.GetBuildNumber(baseTag.ResolvedRef, mergeCommit);
            if (buildNumber == null) throw new CannotDetermineBuildNumberException(baseTag.ResolvedRef, mergeCommit);

            var branchType = new BranchSemantics().GetBranchType(branch);
            return new SemanticVersion(versionNumber, buildNumber.Value, branchType);
        }
    }
}
