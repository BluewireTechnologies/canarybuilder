using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.IntegrationTests;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;
using RefCleaner.Collectors;

namespace RefCleaner.IntegrationTests
{
    [TestFixture]
    public class RemoteBranchProviderTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private GitWorkingCopy remote;

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            remote = await session.Init(Default.TemporaryDirectory, "origin");
            await session.Commit(remote, "Initial comit", CommitOptions.AllowEmptyCommit);
            workingCopy = await session.Clone(remote.GetDefaultRepository().GetLocationUri(), Default.TemporaryDirectory, "repository");
        }

        [Test]
        public async Task GetsOnlyRemoteBranches()
        {
            await session.CreateBranchAndCheckout(remote, "test-remote");
            await session.Commit(remote, "A commit", CommitOptions.AllowEmptyCommit);

            await session.Fetch(workingCopy);
            await session.CreateBranchAndCheckout(workingCopy, "test-local");
            await session.Commit(workingCopy, "A commit", CommitOptions.AllowEmptyCommit);

            var provider = new RemoteBranchProvider(session, workingCopy, "origin");
            var branches = await provider.GetAllBranches();

            Assert.That(branches.Select(b => b.Name).ToArray(), Is.EquivalentTo(new [] { "master", "test-remote" }));
        }

        [Test]
        public async Task DoesNotReturnBranchesMergedLocally()
        {
            await session.CreateBranchAndCheckout(remote, "test");
            await session.Commit(remote, "A commit", CommitOptions.AllowEmptyCommit);

            await session.Fetch(workingCopy);
            await session.Checkout(workingCopy, new Ref("test"));
            await session.Checkout(workingCopy, new Ref("master"));
            await session.Merge(workingCopy, new MergeOptions { FastForward = MergeFastForward.Only }, new Ref("test"));


            var provider = new RemoteBranchProvider(session, workingCopy, "origin");
            var merged = await provider.GetMergedBranches(new Ref("master"));

            Assert.False(merged.Contains(new Ref("test")));
        }

        [Test]
        public async Task DoesNotReturnUnmergedBranches()
        {
            await session.CreateBranchAndCheckout(remote, "test");
            await session.Commit(remote, "A commit", CommitOptions.AllowEmptyCommit);

            await session.Fetch(workingCopy);

            var provider = new RemoteBranchProvider(session, workingCopy, "origin");
            var merged = await provider.GetMergedBranches(new Ref("master"));

            Assert.False(merged.Contains(new Ref("test")));
        }

        [Test]
        public async Task ReturnsBranchesMergedRemotely()
        {
            await session.CreateBranchAndCheckout(remote, "test");
            await session.Commit(remote, "A commit", CommitOptions.AllowEmptyCommit);
            await session.Checkout(remote, new Ref("master"));
            await session.Merge(remote, new MergeOptions { FastForward = MergeFastForward.Only }, new Ref("test"));

            await session.Fetch(workingCopy);

            var provider = new RemoteBranchProvider(session, workingCopy, "origin");
            var merged = await provider.GetMergedBranches(new Ref("master"));

            Assert.True(merged.Contains(new Ref("test")));
        }
    }
}
