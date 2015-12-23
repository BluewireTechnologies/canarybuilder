using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CanaryBuilder.Common.Git
{
    public class GitFinder
    {
        public async Task<Git> FromEnvironment()
        {
            const string binaryName = "git.exe";

            var pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (String.IsNullOrWhiteSpace(pathVariable)) return null;

            var paths = pathVariable.Split(';');
            foreach (var path in paths.Select(Path.GetFullPath))
            {
                var maybeGitPath = Path.Combine(path, binaryName);
                if (File.Exists(maybeGitPath))
                {
                    var git = new Git(maybeGitPath);
                    await git.Validate();
                    return git;
                }
            }
            return null;
        }
    }
}
