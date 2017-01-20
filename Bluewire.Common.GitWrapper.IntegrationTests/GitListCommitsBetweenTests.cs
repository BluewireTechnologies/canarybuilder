using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitListCommitsBetweenTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private RepoStructureBuilder builder;
        private Ref startTag;
        private Ref start;

        private static Ref MasterBranch => new Ref("master");

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "Initial commit", CommitOptions.AllowEmptyCommit);

            builder = new RepoStructureBuilder(session, workingCopy);
            startTag = await session.CreateTag(workingCopy, "start", Ref.Head, "");
            start = await session.ResolveRef(workingCopy, startTag);
        }

        [Test]
        public async Task FirstParentOnly_DoesNotIncludeCommitsOnMergedBranches()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 1);
            await builder.AddCommitsToBranch("master", 5);

            var branchCommit = await session.ResolveRef(workingCopy, new Ref("branch"));
            await session.Merge(workingCopy, new Ref("branch"));

            var firstParents = await session.ListCommitsBetween(workingCopy, startTag, MasterBranch, new ListCommitsOptions { FirstParentOnly = true });

            Assert.That(firstParents, Does.Not.Contains(branchCommit));
        }

        [Test]
        public async Task FirstParentOnly_IncludesEndAndAllCommitsBetween_ButExcludesStart()
        {
            await builder.AddCommitsToBranch("master", 5);

            var firstParents = await session.ListCommitsBetween(workingCopy, startTag, MasterBranch);

            var end = await session.ResolveRef(workingCopy, MasterBranch);

            Assert.That(firstParents, Does.Not.Contains(start));
            Assert.That(firstParents, Contains.Item(end));
            Assert.That(firstParents, Has.Length.EqualTo(5));
        }

        [Test]
        public async Task FirstParentOnly_DoesIncludeMergeCommits()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 1);
            await builder.AddCommitsToBranch("master", 3);

            await session.Merge(workingCopy, new Ref("branch"));
            var mergeCommit = await session.ResolveRef(workingCopy, MasterBranch);

            await builder.AddCommitsToBranch("master", 2);

            var firstParents = await session.ListCommitsBetween(workingCopy, startTag, MasterBranch);

            Assert.That(firstParents, Contains.Item(mergeCommit));
        }
    }
}
