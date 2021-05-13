using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitWorkingCopyTests
    {
        [Test]
        public async Task CanFindWorkingCopyFromSubdirectoryPath()
        {
            var session = await Default.GitSession();
            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");

            var subdirectoryPath = Path.Combine(workingCopy.Root, "some", "subdirectory");
            Directory.CreateDirectory(subdirectoryPath);

            var result = await session.FindWorkingCopyContaining(subdirectoryPath);

            Assert.That(result.Root, Is.EqualTo(workingCopy.Root).IgnoreCase);
        }

        [Test]
        public async Task CanFindWorkingCopyFromFilePath()
        {
            var session = await Default.GitSession();
            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");

            var subdirectoryPath = Path.Combine(workingCopy.Root, "some", "subdirectory");
            Directory.CreateDirectory(subdirectoryPath);
            var filePath = Path.Combine(subdirectoryPath, "filename");
            File.WriteAllText(filePath, "Test");

            var result = await session.FindWorkingCopyContaining(filePath);

            Assert.That(result.Root, Is.EqualTo(workingCopy.Root).IgnoreCase);
        }

        [Test]
        public async Task ReturnsNullWorkingCopyIfPathIsNotInsideOne()
        {
            var session = await Default.GitSession();
            var path = Path.Combine(Default.TemporaryDirectory, "not-a-repository");
            Directory.CreateDirectory(path);

            var result = await session.FindWorkingCopyContaining(path);

            Assert.That(result, Is.Null);
        }
    }
}
