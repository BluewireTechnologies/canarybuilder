using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Common.Git.IntegrationTests
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
            Assert.False(await session.IsClean(workingCopy));
        }
    }
}
