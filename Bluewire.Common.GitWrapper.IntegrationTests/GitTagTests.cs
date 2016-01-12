using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitTagTests
    {
        [Test]
        public async Task CanCreateTagFromSpecificRef()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);
            var firstCommit = await session.ResolveRef(workingCopy, Ref.Head);
            await session.Commit(workingCopy, "second commit", CommitOptions.AllowEmptyCommit);

            var tag = await session.CreateTag(workingCopy, "new-tag", firstCommit, "Test tag");

            Assert.That(await session.AreRefsEquivalent(workingCopy, tag, firstCommit), Is.True);
        }
        
        [Test]
        public async Task CanDetectExistingTagRef()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            var tag = await session.CreateTag(workingCopy, "new-tag", Ref.Head, "Test tag");

            Assert.That(await session.RefExists(workingCopy, tag), Is.True);
        }

        [Test]
        public async Task CanDetectNotExistingTagRef()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            Assert.That(await session.RefExists(workingCopy, new Ref("does-not-exist")), Is.False);
        }
    }
}
