using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.IntegrationTests;
using Bluewire.Common.GitWrapper.IntegrationTests.TestInfrastructure;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Tools.GitRepository.IntegrationTests
{
    [TestFixture, Explicit]
    public class TopologicalBuildNumberResolverPerformanceTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;
        private RepoStructureBuilder builder;
        private Ref startTag;
        private Ref buildNumber230;

        private static Ref MasterBranch => new Ref("master");

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "Initial commit", CommitOptions.AllowEmptyCommit);

            builder = new RepoStructureBuilder(session, workingCopy);
            startTag = await session.CreateTag(workingCopy, "start", Ref.Head, "");

            await builder.AddCommitsToBranch("master", 230);
            buildNumber230 = await session.ResolveRef(workingCopy, MasterBranch);
            await builder.AddCommitsToBranch("master", 270);
        }

        [Test]
        public async Task BinarySearch()
        {
            var sut = new TopologicalBuildNumberResolver(session)
            {
                SearchImplementation = new TopologicalBuildNumberResolver.BinarySearcherImpl()
            };

            var commit = await sut.FindCommit(workingCopy, startTag, MasterBranch, 230);

            Assert.That(commit, Is.EqualTo(buildNumber230));
        }

        [Test]
        public async Task LinearSearch()
        {
            var sut = new TopologicalBuildNumberResolver(session)
            {
                SearchImplementation = new TopologicalBuildNumberResolver.LinearSearcherImpl()
            };

            var commit = await sut.FindCommit(workingCopy, startTag, MasterBranch, 230);

            Assert.That(commit, Is.EqualTo(buildNumber230));
        }
    }
}
