using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitReadBlobTests
    {
        private GitSession session;
        private GitWorkingCopy workingCopy;

        private static Ref MasterBranch => new Ref("master");

        [SetUp]
        public async Task SetUp()
        {
            session = await Default.GitSession();
            workingCopy = await session.Init(Default.TemporaryDirectory, "repository");
            await session.Commit(workingCopy, "Initial commit", CommitOptions.AllowEmptyCommit);
        }

        [Test]
        public async Task CanReadBlobContainingText()
        {
            var original = "contents\nline 2";
            File.WriteAllText(workingCopy.Path("testfile"), original);
            await session.AddFile(workingCopy, "testfile");
            await session.Commit(workingCopy, "test");

            var paths = await session.ListPaths(workingCopy, MasterBranch, new ListPathsOptions { Mode = ListPathsOptions.ListPathsMode.Recursive });
            var objectName = paths.Single().ObjectName;

            using (var stream = new MemoryStream())
            {
                await session.ReadBlob(workingCopy, objectName, stream);
                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    var roundtripped = await reader.ReadToEndAsync();
                    Assert.That(roundtripped, Is.EqualTo(original));
                }
            }
        }

        [Test]
        public async Task CanReadBlobContainingBinary()
        {
            var original = new byte[4096];
            new Random().NextBytes(original);
            File.WriteAllBytes(workingCopy.Path("testfile"), original);
            await session.AddFile(workingCopy, "testfile");
            await session.Commit(workingCopy, "test");

            var paths = await session.ListPaths(workingCopy, MasterBranch, new ListPathsOptions { Mode = ListPathsOptions.ListPathsMode.Recursive });
            var objectName = paths.Single().ObjectName;

            using (var stream = new MemoryStream())
            {
                await session.ReadBlob(workingCopy, objectName, stream);
                stream.Position = 0;
                var roundtripped = stream.ToArray();
                Assert.That(roundtripped, Is.EqualTo(original));
            }
        }
    }
}
