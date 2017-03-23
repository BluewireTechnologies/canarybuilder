using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.IntegrationTests;
using Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Tools.GitRepository.IntegrationTests
{
    [TestFixture]
    public class TopologicalBuildNumberResolverTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private TopologicalBuildNumberResolver sut;
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

            sut = new TopologicalBuildNumberResolver(session);
            builder = new RepoStructureBuilder(session, workingCopy);
            startTag = await session.CreateTag(workingCopy, "start", Ref.Head, "");
            start = await session.ResolveRef(workingCopy, startTag);
        }

        [Test]
        public async Task NegativeBuildNumberIsInvalid()
        {
            await builder.AddCommitsToBranch("master", 1);

            Assert.ThrowsAsync<BuildNumberOutOfRangeException>(async () => await sut.FindCommit(workingCopy, startTag, MasterBranch, -1));
        }

        [Test]
        public async Task PositiveBuildNumberLargerThanThatOfEndIsInvalid()
        {
            await builder.AddCommitsToBranch("master", 1);

            Assert.ThrowsAsync<BuildNumberOutOfRangeException>(async () => await sut.FindCommit(workingCopy, startTag, MasterBranch, 2));
        }

        [Test]
        public async Task BuildNumberZeroResolvesToIdentityOfStart()
        {
            await builder.AddCommitsToBranch("master", 1);

            var resolved = await sut.FindCommit(workingCopy, startTag, MasterBranch, 0);

            Assert.That(resolved, Is.EqualTo(start));
        }

        [Test]
        public async Task BuildNumberOfEndResolvesToIdentityOfEnd()
        {
            await builder.AddCommitsToBranch("master", 8);

            var resolved = await sut.FindCommit(workingCopy, startTag, MasterBranch, 8);

            var end = await session.ResolveRef(workingCopy, MasterBranch);
            Assert.That(resolved, Is.EqualTo(end));
        }

        [Test]
        public async Task BuildNumberNotInFirstParentAncestryIsInvalid()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 3);
            await builder.AddCommitsToBranch("master", 8);
            await session.Merge(workingCopy, new Ref("branch"));

            Assert.ThrowsAsync<BuildNumberNotFoundException>(() => sut.FindCommit(workingCopy, startTag, MasterBranch, 9));
        }

        [Test]
        public async Task BuildNumberInFirstParentAncestryResolves()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 3);
            await builder.AddCommitsToBranch("master", 8);

            var buildNumber8 = await session.ResolveRef(workingCopy, MasterBranch);
            await session.Merge(workingCopy, new Ref("branch"));

            var resolved = await sut.FindCommit(workingCopy, startTag, MasterBranch, 8);

            Assert.That(resolved, Is.EqualTo(buildNumber8));
        }

        [Test]
        public async Task BuildNumberOfMergeCommitInFirstParentAncestryResolves()
        {
            await builder.CreateBranchWithCommits(startTag, "branch", 3);
            await builder.AddCommitsToBranch("master", 8);

            await session.Merge(workingCopy, new Ref("branch"));
            var mergeCommit = await session.ResolveRef(workingCopy, MasterBranch);

            await builder.AddCommitsToBranch("master", 3);

            var resolved = await sut.FindCommit(workingCopy, startTag, MasterBranch, 12);

            Assert.That(resolved, Is.EqualTo(mergeCommit));
        }
    }
}
