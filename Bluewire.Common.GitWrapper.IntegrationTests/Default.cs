using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    public static class Default
    {
        public static async Task<GitSession> GitSession()
        {
            var logger = new SimpleConsoleInvocationLogger(TestContext.Out);
            return new GitSession(await new GitFinder(logger).FromEnvironment(), logger);
        }

        public static string TemporaryDirectory => Console.NUnit3.Filesystem.TemporaryDirectory.ForCurrentTest();
    }
}
