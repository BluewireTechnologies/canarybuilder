using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.IntegrationTests;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Tools.GitRepository.IntegrationTests
{
    [TestFixture]
    public class TopologicalBuildNumberCalculatorTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "Initial commit", CommitOptions.AllowEmptyCommit);
        }

        [Test]
        public async Task StartCommitHasBuildNumberOfZero()
        {
            var buildNumber = await GetBuildNumber(Ref.Head, Ref.Head);
            Assert.That(buildNumber, Is.EqualTo(0));
        }

        [Test]
        public async Task FirstCommitHasBuildNumberOfOne()
        {
            var start = await session.ResolveRef(workingCopy, Ref.Head);

            await session.Commit(workingCopy, "Commit 1", CommitOptions.AllowEmptyCommit);

            var buildNumber = await GetBuildNumber(start, Ref.Head);
            Assert.That(buildNumber, Is.EqualTo(1));
        }

        [Test]
        public async Task MergingThreeCommitsBasedOnStartIncrementsBuildNumberByFour()
        {
            await session.CreateBranchAndCheckout(workingCopy, "test-branch");
            await session.Commit(workingCopy, "Branch Commit 1", CommitOptions.AllowEmptyCommit);
            await session.Commit(workingCopy, "Branch Commit 2", CommitOptions.AllowEmptyCommit);
            await session.Commit(workingCopy, "Branch Commit 3", CommitOptions.AllowEmptyCommit);

            await session.Checkout(workingCopy, new Ref("master"));

            await session.Merge(workingCopy, new MergeOptions { FastForward = MergeFastForward.Never }, new Ref("test-branch"));

            var buildNumber = await GetBuildNumber(Ref.Head.Parent(), Ref.Head);
            Assert.That(buildNumber, Is.EqualTo(4));
        }

        [Test]
        public async Task MergingThreeCommitsBasedBeforeStartIncrementsBuildNumberByFour()
        {
            await session.CreateBranchAndCheckout(workingCopy, "test-branch");
            await session.Commit(workingCopy, "Branch Commit 1", CommitOptions.AllowEmptyCommit);
            await session.Commit(workingCopy, "Branch Commit 2", CommitOptions.AllowEmptyCommit);
            await session.Commit(workingCopy, "Branch Commit 3", CommitOptions.AllowEmptyCommit);

            await session.Checkout(workingCopy, new Ref("master"));
            await session.Commit(workingCopy, "Start Commit", CommitOptions.AllowEmptyCommit);

            await session.Merge(workingCopy, new MergeOptions { FastForward = MergeFastForward.Never }, new Ref("test-branch"));

            var buildNumber = await GetBuildNumber(Ref.Head.Parent(), Ref.Head);
            Assert.That(buildNumber, Is.EqualTo(4));
        }

        private async Task<int?> GetBuildNumber(Ref start, Ref end)
        {
            var reference = await new TopologicalBuildNumberCalculator(session).GetBuildNumber(workingCopy, start, end);

            var sut = new TopologicalBuildNumberProvider(session, workingCopy);
            var buildNumber = await sut.GetBuildNumber(start, end);

            Assert.That(buildNumber, Is.EqualTo(reference));
            return buildNumber;
        }
    }
}
