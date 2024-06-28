using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    public static class Default
    {
        public static async Task<GitSession> GitSession()
        {
            // Logger can change behaviour. Run CI tests without it.
#if DEBUG
            var logger = new TestConsoleInvocationLogger(TestContext.Out);
            return new GitSession(await new GitFinder(logger).FromEnvironment(), logger);
#else
            return new GitSession(await new GitFinder().FromEnvironment());
#endif
        }

        public static string TemporaryDirectory => Console.NUnit3.Filesystem.TemporaryDirectory.ForCurrentTest();
    }
}
