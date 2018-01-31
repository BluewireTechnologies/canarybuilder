using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitRepositoryTests
    {
        [Test]
        public async Task GetLocationUriCreatesCorrectUriForLocalPath()
        {
            var session = await Default.GitSession();
            var workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            var repository = workingCopy.GetDefaultRepository();

            var localPath = workingCopy.Root;
            // Expect something like 'file:///<drive>:/<working-copy-path>/.git'
            var expectedUri = new Uri($"file:///{localPath.Replace(Path.DirectorySeparatorChar, '/')}/.git");

            Assert.That(repository.GetLocationUri(), Is.EqualTo(expectedUri));
        }
    }
}
