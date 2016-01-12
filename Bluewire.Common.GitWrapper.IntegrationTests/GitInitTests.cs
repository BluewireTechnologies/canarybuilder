using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitInitTests
    {
        [Test]
        public async Task CanCreateNewRepositoryAndWorkingCopy()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            Assert.That(Directory.Exists(workingCopy.Root));
            Assert.That(Directory.Exists(Path.Combine(workingCopy.Root, ".git")));
        }

        [Test]
        public async Task NewWorkingCopyIsClean()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");

            Assert.True(await session.IsClean(workingCopy));
        }

        [Test]
        public async Task NewWorkingCopyWithUnstagedFileIsNotClean()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");

            File.AppendAllText(workingCopy.Path("testfile"), "contents");

            Assert.False(await session.IsClean(workingCopy));
        }
    }
}
