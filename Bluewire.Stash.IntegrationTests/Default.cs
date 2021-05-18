using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Stash.IntegrationTests.TestInfrastructure;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests
{
    public static class Default
    {
        public static async Task<GitSession> GitSession()
        {
            var logger = new TestConsoleInvocationLogger(TestContext.Out);
            return new GitSession(await new GitFinder(logger).FromEnvironment(), logger);
        }

        public static string TemporaryDirectory => Common.Console.NUnit3.Filesystem.TemporaryDirectory.ForCurrentTest();
    }
}
