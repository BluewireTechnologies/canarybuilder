using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure
{
    public class RepoStructureBuilder
    {
        private readonly GitSession session;
        private readonly GitWorkingCopy workingCopy;

        public RepoStructureBuilder(GitSession session, GitWorkingCopy workingCopy)
        {
            this.session = session;
            this.workingCopy = workingCopy;
        }

        /// <summary>
        /// Create a new branch 'branchName' based on 'start' with 'commitCount' empty commits.
        /// Leaves 'branchName' checked out.
        /// </summary>
        public async Task CreateBranchWithCommits(Ref start, string branchName, int commitCount)
        {
            await session.CreateBranchAndCheckout(workingCopy, branchName, start);
            for (var i = 0; i < commitCount; i++)
            {
                await session.Commit(workingCopy, $"Create {branchName} commit {i + 1}", CommitOptions.AllowEmptyCommit);
            }
        }

        /// <summary>
        /// Add 'commitCount' empty commits to an existing branch 'branchName'.
        /// Leaves 'branchName' checked out.
        /// </summary>
        public async Task AddCommitsToBranch(string branchName, int commitCount)
        {
            await session.Checkout(workingCopy, new Ref(branchName));
            for (var i = 0; i < commitCount; i++)
            {
                await session.Commit(workingCopy, $"Add {branchName} commit {i + 1}", CommitOptions.AllowEmptyCommit);
            }
        }
    }
}
