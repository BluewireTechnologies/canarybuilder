using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.IntegrationTests
{
    public static class Default
    {
        public static async Task<GitSession> GitSession()
        {
            return new GitSession(await new GitFinder().FromEnvironment(), new SimpleConsoleInvocationLogger(TestContext.Out));
        }

        public static string TemporaryDirectory => Console.NUnit3.Filesystem.TemporaryDirectory.ForCurrentTest();
    }
}
