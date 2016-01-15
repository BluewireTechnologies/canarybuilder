using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitBranchTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private GitRepository repository;

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            repository = workingCopy.GetDefaultRepository();
        }

        [Test]
        public async Task NewRepositoryContainsMasterBranchAfterFirstComment()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            var branches = await session.ListBranches(workingCopy);

            Assert.That(branches, Is.EquivalentTo(new[] { new Ref("master") }));
        }


        [Test]
        public async Task CanCreateBranch()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            var branch = await session.CreateBranch(workingCopy, "new-branch");

            var branches = await session.ListBranches(workingCopy);
            Assert.That(branch, Is.EqualTo(new Ref("new-branch")));
            Assert.That(branches, Is.EquivalentTo(new[] { branch, new Ref("master") }));
        }

        [Test]
        public async Task CanCreateBranchWithoutWorkingCopy()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            var branch = await session.CreateBranch(repository, "new-branch");

            var branches = await session.ListBranches(repository);
            Assert.That(branch, Is.EqualTo(new Ref("new-branch")));
            Assert.That(branches, Is.EquivalentTo(new[] { branch, new Ref("master") }));
        }

        [Test]
        public async Task CanCreateBranchFromSpecificRef()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommit = await session.ResolveRef(workingCopy, Ref.Head);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var branch = await session.CreateBranch(workingCopy, "new-branch", firstCommit);

            Assert.That(await session.AreRefsEquivalent(workingCopy, branch, firstCommit), Is.True);
        }

        [Test]
        public async Task CanCreateAndCheckoutBranchFromSpecificRef()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommit = await session.ResolveRef(workingCopy, Ref.Head);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var branch = await session.CreateBranchAndCheckout(workingCopy, "new-branch", firstCommit);

            Assert.That(await session.GetCurrentBranch(workingCopy), Is.EqualTo(branch));
            Assert.That(await session.AreRefsEquivalent(workingCopy, Ref.Head, firstCommit), Is.True);
        }

        [Test]
        public async Task CanDetectExistingBranchRef()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            var branch = await session.CreateBranch(workingCopy, "new-branch");

            Assert.That(await session.BranchExists(workingCopy, branch), Is.True);
        }

        [Test]
        public async Task CanDetectNotExistingBranchRef()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            Assert.That(await session.BranchExists(workingCopy, new Ref("does-not-exist")), Is.False);
        }

        [Test]
        public async Task CanListOnlyUnmergedBranches()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommit = await session.ResolveRef(workingCopy, Ref.Head);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var branch = await session.CreateBranchAndCheckout(workingCopy, "new-branch", firstCommit);

            Assert.That(await session.ListBranches(workingCopy, new ListBranchesOptions { UnmergedWith = new Ref("master") }), Is.Empty);
            Assert.That(await session.ListBranches(workingCopy, new ListBranchesOptions { UnmergedWith = branch }), Is.EqualTo(new[] { new Ref("master") }));
        }
    }
}
