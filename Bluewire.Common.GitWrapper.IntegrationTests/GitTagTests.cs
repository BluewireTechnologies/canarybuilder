using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitTagTests
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
        public async Task CanCreateTagFromSpecificRef()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommit = await session.ResolveRef(workingCopy, Ref.Head);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var tag = await session.CreateTag(workingCopy, "new-tag", firstCommit, "Test tag");

            Assert.That(await session.AreRefsEquivalent(workingCopy, tag, firstCommit), Is.True);
        }

        [Test]
        public async Task CanCreateTagFromSpecificRefWithoutWorkingCopy()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommit = await session.ResolveRef(workingCopy, Ref.Head);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var tag = await session.CreateTag(repository, "new-tag", firstCommit, "Test tag");

            Assert.That(await session.AreRefsEquivalent(repository, tag, firstCommit), Is.True);
        }

        [Test]
        public async Task CanDetectExistingTagRef()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            var tag = await session.CreateTag(workingCopy, "new-tag", Ref.Head, "Test tag");

            Assert.That(await session.RefExists(workingCopy, tag), Is.True);
        }

        [Test]
        public async Task CanDetectNotExistingTagRef()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            Assert.That(await session.RefExists(workingCopy, new Ref("does-not-exist")), Is.False);
        }
    }
}
