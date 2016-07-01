using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Common.GitWrapper.Parsing;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitDiffTests
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
            await session.Commit(workingCopy, "first commit", CommitOptions.AllowEmptyCommit);

            File.AppendAllText(workingCopy.Path("testfile"), "contents");

            await session.AddFile(workingCopy, "testfile");

            var diff = await session.Diff(workingCopy, new DiffOptions { Cached = true });

            var fileDiff = diff.Single(f => f.Path == "testfile");

            Assert.That(fileDiff.Chunks.Single().Inserted, Is.EqualTo(new [] { new DiffChunkLine { Line = 1, Value = "contents" } }));
        }
    }
}
