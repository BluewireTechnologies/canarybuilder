using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper
{
    public class GitFinder
    {
        private readonly IConsoleInvocationLogger logger;

        public GitFinder(IConsoleInvocationLogger logger = null)
        {
            this.logger = logger;
        }

        public async Task<Git> FromEnvironment() =>
            await FromEnvironment(Environment.GetEnvironmentVariable("PATH"));

        public async Task<Git> FromEnvironment(string pathVariableValue)
        {
            if (String.IsNullOrWhiteSpace(pathVariableValue)) return null;
            var paths = new List<string>();
            foreach (var path in pathVariableValue.Split(';'))
            {
                try
                {
                    paths.Add(Path.GetFullPath(path));
                }
                catch
                {
                    /* ignore */
                }
            }
            return await FromCandidatePathsInternal(paths);
        }

        public async Task<Git> FromCandidatePaths(string[] paths) =>
            await FromCandidatePathsInternal(paths.Select(Path.GetFullPath).ToArray());

        private async Task<Git> FromCandidatePathsInternal(IEnumerable<string> paths)
        {
            const string binaryName = "git.exe";

            foreach (var path in paths)
            {
                var maybeGitPath = Path.Combine(path, binaryName);
                if (File.Exists(maybeGitPath))
                {
                    var git = new Git(maybeGitPath);
                    await git.Validate(logger);
                    return git;
                }
            }
            return null;
        }
    }
}
