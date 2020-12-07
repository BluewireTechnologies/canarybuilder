using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    [TestFixture]
    public class GitDetectionTests
    {
        [Test]
        public async Task GitCanBeFoundOnThisMachine()
        {
            var git = await new GitFinder().FromEnvironment();
            Assert.That(git, Is.Not.Null);
        }

        [Test]
        public async Task RequestingGitVersionSucceeds()
        {
            var git = await new GitFinder().FromEnvironment();

            var versionString = await git.GetVersionString(new TestConsoleInvocationLogger(new StringWriter()));

            Assert.That(versionString, Is.Not.Null);
            Assert.That(versionString, Does.Match(@"\d\.\d"));
        }
    }
}
