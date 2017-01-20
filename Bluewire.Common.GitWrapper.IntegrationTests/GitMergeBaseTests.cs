using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitMergeBaseTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
        }

        [Test]
        public async Task FirstCommitIsItsOwnMergeBase()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            var firstCommit = await session.ResolveRef(workingCopy, Ref.Head);
            var mergeBase = await session.MergeBase(workingCopy, firstCommit, firstCommit);

            Assert.That(mergeBase, Is.EqualTo(firstCommit));
        }

        [Test]
        public async Task CommitParentIsMergeBaseOfParentAndCommit()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var commit = await session.ResolveRef(workingCopy, Ref.Head);
            var mergeBase = await session.MergeBase(workingCopy, commit.Parent(), commit);

            Assert.That(mergeBase, Is.EqualTo(await session.ResolveRef(workingCopy, commit.Parent())));
        }

        [Test]
        public async Task CommitParentIsMergeBaseOfCommitAndParent()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var commit = await session.ResolveRef(workingCopy, Ref.Head);
            var mergeBase = await session.MergeBase(workingCopy, commit, commit.Parent());

            Assert.That(mergeBase, Is.EqualTo(await session.ResolveRef(workingCopy, commit.Parent())));
        }

        [Test]
        public async Task CommitParentIsAncestorOfCommit()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var commit = await session.ResolveRef(workingCopy, Ref.Head);
            Assert.True(await session.IsAncestor(workingCopy, commit.Parent(), commit));
        }

        [Test]
        public async Task CommitIsNotAncestorOfCommitParent()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var commit = await session.ResolveRef(workingCopy, Ref.Head);
            Assert.False(await session.IsAncestor(workingCopy, commit, commit.Parent()));
        }
    }
}
