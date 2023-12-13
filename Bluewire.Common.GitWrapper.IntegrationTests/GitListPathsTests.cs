using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitListPathsTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private RepoStructureBuilder builder;
        private Ref startTag;

        private static Ref MasterBranch => new Ref("master");

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "Initial commit", CommitOptions.AllowEmptyCommit);

            builder = new RepoStructureBuilder(session, workingCopy);
            startTag = await session.CreateTag(workingCopy, "start", Ref.Head, "");
        }

        [Test]
        public async Task CanListPathsOnCurrentHead()
        {
            File.AppendAllText(workingCopy.Path("testfile"), "contents");
            await session.AddFile(workingCopy, "testfile");
            await session.Commit(workingCopy, "test");

            await builder.AddCommitsToBranch("master", 5);

            var paths = await session.ListPaths(workingCopy, MasterBranch, new ListPathsOptions { Mode = ListPathsOptions.ListPathsMode.Recursive });

            Assert.That(paths, Has.Length.EqualTo(1));
            Assert.That(paths.Single().ObjectType, Is.EqualTo(ObjectType.Blob));
            Assert.That(paths.Single().Path, Is.EqualTo("testfile"));
        }

        [Test]
        public async Task CanListPathsOnBranch()
        {
            File.AppendAllText(workingCopy.Path("testfile"), "contents");
            await session.AddFile(workingCopy, "testfile");
            await session.Commit(workingCopy, "test");

            await builder.AddCommitsToBranch("master", 5);

            await session.Checkout(workingCopy, startTag);

            var paths = await session.ListPaths(workingCopy, MasterBranch, new ListPathsOptions { Mode = ListPathsOptions.ListPathsMode.Recursive });

            Assert.That(paths, Has.Length.EqualTo(1));
            Assert.That(paths.Single().ObjectType, Is.EqualTo(ObjectType.Blob));
            Assert.That(paths.Single().Path, Is.EqualTo("testfile"));
        }
    }
}
