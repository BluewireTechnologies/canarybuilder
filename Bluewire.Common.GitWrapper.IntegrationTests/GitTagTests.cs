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

            Assert.That(await session.TagExists(workingCopy, tag), Is.True);
        }

        [Test]
        public async Task CanDetectNotExistingTagRef()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            Assert.That(await session.TagExists(workingCopy, new Ref("does-not-exist")), Is.False);
        }

        [Test]
        public async Task CanGetTagDetails()
        {
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommit = await session.ResolveRef(workingCopy, Ref.Head);

            var tag = await session.CreateTag(workingCopy, "new-tag", Ref.Head, "Test tag");

            var details = await session.ReadTagDetails(workingCopy, tag);

            Assert.That(details.Ref, Is.EqualTo(new Ref("refs/tags/new-tag")));
            Assert.That(details.Name, Is.EqualTo("new-tag"));
            Assert.That(details.ResolvedRef, Is.EqualTo(firstCommit));
            Assert.That(details.Message, Is.EqualTo("Test tag\r\n"));
        }

        [Test]
        public async Task CanCreateTagWithMultilineMessage()
        {
            const string message = @"Test tag

Multiple line commit message
with whitespace lines too.
";
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var tag = await session.CreateTag(workingCopy, "new-tag", Ref.Head, message);

            var details = await session.ReadTagDetails(workingCopy, tag);

            Assert.That(details.Message, Is.EqualTo(message));
        }
    }
}
