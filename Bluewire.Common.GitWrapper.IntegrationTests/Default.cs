using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    public static class Default
    {
        public static async Task<GitSession> GitSession()
        {
            var logger = new TestConsoleInvocationLogger(TestContext.Out);
            return new GitSession(await new GitFinder(logger).FromEnvironment(), logger);
        }

        public static string TemporaryDirectory => Console.NUnit3.Filesystem.TemporaryDirectory.ForCurrentTest();
    }
}
