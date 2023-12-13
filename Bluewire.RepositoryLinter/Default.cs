using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;

namespace Bluewire.RepositoryLinter
{
    public static class Default
    {
        public static async Task<GitSession> GitSession()
        {
            return new GitSession(await new GitFinder().FromEnvironment());
        }

        public static string TemporaryDirectory => Common.Console.NUnit3.Filesystem.TemporaryDirectory.ForCurrentTest();
    }
}
