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

        [Test]
        public async Task NewWorkingCopyHasRepository()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");

            Assert.That(workingCopy.GetDefaultRepository(), Is.Not.Null);
        }

        [Test]
        public async Task CanFindRepositoryInWorkingCopy()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");

            var repository = GitRepository.Find(workingCopy.Root);
            Assert.That(repository.Location, Is.EqualTo(workingCopy.GetDefaultRepository().Location));
        }

        [Test]
        public async Task CanFindRepositoryInWorkingCopyRepository()
        {
            var session = await Default.GitSession();

            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");

            var repository = GitRepository.Find(workingCopy.Path(".git"));
            Assert.That(repository.Location, Is.EqualTo(workingCopy.GetDefaultRepository().Location));
        }
    }
}
