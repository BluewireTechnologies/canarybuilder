using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using NUnit.Framework;

namespace Bluewire.RepositoryLinter
{
    public static class Default
    {
        public static async Task<GitSession> GitSession()
        {
            return new GitSession(await new GitFinder().FromEnvironment());
        }

        public static async Task<GitSession> LoggedGitSession()
        {
            var logger = new TestConsoleInvocationLogger(TestContext.Out);
            return new GitSession(await new GitFinder().FromEnvironment(), logger);
        }

        public static string TemporaryDirectory => Common.Console.NUnit3.Filesystem.TemporaryDirectory.ForCurrentTest();
    }
}
