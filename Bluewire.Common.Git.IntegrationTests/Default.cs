using System.Threading.Tasks;
using Bluewire.Common.Git.IntegrationTests.TestInfrastructure;
using NUnit.Framework;

namespace Bluewire.Common.Git.IntegrationTests
{
    public static class Default
    {
        public static async Task<GitSession> GitSession()
        {
            return new GitSession(await new GitFinder().FromEnvironment(), new TestConsoleInvocationLogger(TestContext.Out));
        }

        public static string TemporaryDirectory => TemporaryDirectoryForTest.Allocate(TestContext.CurrentContext);
    }
}
