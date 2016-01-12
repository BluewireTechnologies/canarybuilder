using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitAddTests
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
        public async Task AddingFileStagesIt()
        {
            File.AppendAllText(workingCopy.Path("testfile"), "contents");

            await session.AddFile(workingCopy, "testfile");

            var status = await session.Status(workingCopy);
            Assert.That(status, Is.EquivalentTo(new[] {
                new GitStatusEntry { Path = "testfile", IndexState = IndexState.Added, WorkTreeState = WorkTreeState.Unmodified }
            }).Using(GitStatusEntry.EqualityComparer));
        }
    }
}
