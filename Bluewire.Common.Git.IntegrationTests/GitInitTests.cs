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
    }
}
