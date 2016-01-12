using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitAddTests
    {
        [Test]
        public async Task AddingFileStagesIt()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");

            File.AppendAllText(workingCopy.Path("testfile"), "contents");

            await session.AddFile(workingCopy, "testfile");

            var status = await session.Status(workingCopy);
            Assert.That(status, Is.EquivalentTo(new[] {
                new GitStatusEntry { Path = "testfile", IndexState = IndexState.Added, WorkTreeState = WorkTreeState.Unmodified }
            }).Using(GitStatusEntry.EqualityComparer));
        }
    }
}
