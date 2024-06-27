using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitReadFileTests
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
        public async Task CanReadFile()
        {
            var original = new byte[4096];
            new Random().NextBytes(original);
            File.WriteAllBytes(workingCopy.Path("testfile"), original);
            await session.AddFile(workingCopy, "testfile");
            await session.Commit(workingCopy, "test");

            await session.Checkout(workingCopy, Ref.Head.Parent());

            using (var stream = new MemoryStream())
            {
                await session.ReadFile(workingCopy, MasterBranch, "testfile", stream);
                stream.Position = 0;
                var roundtripped = stream.ToArray();
                Assert.That(roundtripped, Is.EqualTo(original));
            }
        }

        [Test]
        public async Task CanCheckFileExists()
        {
            var original = new byte[4096];
            new Random().NextBytes(original);
            File.WriteAllBytes(workingCopy.Path("testfile"), original);
            await session.AddFile(workingCopy, "testfile");
            await session.Commit(workingCopy, "test");

            await session.Checkout(workingCopy, Ref.Head.Parent());

            Assert.That(await session.FileExists(workingCopy, MasterBranch, "testfile"), Is.True);
            Assert.That(await session.FileExists(workingCopy, MasterBranch, "doesnotexist"), Is.False);
        }
    }
}
