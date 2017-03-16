using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.Console;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.GitRepository;

namespace Bluewire.Tools.Runner.FindBuild
{
    public class BuildVersionFinder
    {
        private readonly GitSession gitSession;
        private readonly Common.GitWrapper.GitRepository repository;

        public BuildVersionFinder(GitSession gitSession, Common.GitWrapper.GitRepository repository)
        {
            if (gitSession == null) throw new ArgumentNullException(nameof(gitSession));
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            this.gitSession = gitSession;
            this.repository = repository;
        }

        public async Task<string[]> GetBuildVersionsFromCommit(Ref commitHash, StructuredBranch[] targetBranches)
        {
            try
            {
                var inspector = new RepositoryStructureInspector(gitSession);
                var integrationPoints = await inspector.QueryIntegrationPoints(repository, commitHash, targetBranches);

                var buildNumbers = new List<string>();
                foreach (var group in integrationPoints.GroupBy(i => i.IntegrationPoint, i => i.TargetBranch))
                {
                    var branch = GetPreferredBranch(group);
                    buildNumbers.Add(await GetBuildNumberFromIntegrationPoint(group.Key, branch));
                }
                return buildNumbers.ToArray();
            }
            catch (RepositoryStructureException ex)
            {
                throw new ErrorWithReturnCodeException(3, ex.Message);
            }
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
                if (BranchType.Master.Equals(type)) return 5;
                if (BranchType.Release.Equals(type)) return 3;
                if (BranchType.ReleaseCandidate.Equals(type)) return 1;
                return 0;
            }
        }

        private async Task<string> GetBuildNumberFromIntegrationPoint(Ref mergeCommit, StructuredBranch branch)
        {
            var inspector = new RepositoryStructureInspector(gitSession);

            var sprintNumber = SprintNumber.Parse(branch.ToString());
            var versionNumber = await inspector.GetActiveVersionNumber(repository, mergeCommit);
            var baseTag = await inspector.ResolveBaseTagForVersion(repository, versionNumber);

            if (sprintNumber != null) Debug.Assert(sprintNumber == Version.Parse(versionNumber));

            var calculator = new TopologicalBuildNumberCalculator(gitSession);
            var buildNumber = await calculator.GetBuildNumber(repository, baseTag.ResolvedRef, mergeCommit);
            if (buildNumber == null) throw new ErrorWithReturnCodeException(4,  "Could not determine build number for the commit. Is the graph properly connected?");

            var branchType = new BranchSemantics().GetBranchType(branch);
            return CreateBuildVersion(versionNumber, buildNumber.Value, branchType);
        }

        private static string CreateBuildVersion(string versionNumber, int buildNumber, BranchType branchType)
        {
            var versionBuilder = new StringBuilder(64);
            versionBuilder.Append(versionNumber);
            versionBuilder.Append(".");
            versionBuilder.Append(buildNumber);
            if (!String.IsNullOrEmpty(branchType.SemanticTag))
            {
                versionBuilder.Append("-");
                versionBuilder.Append(branchType.SemanticTag);
            }
            return versionBuilder.ToString();
        }
    }
}
