using System.IO;

namespace Bluewire.Common.Git
{
    /// <summary>
    /// Location of a Git repository.
    /// </summary>
    public class GitRepository
    {
        private readonly string gitDirPath;

        public GitRepository(string gitDirPath)
        {
            this.gitDirPath = gitDirPath;
            if (!Directory.Exists(gitDirPath)) throw new DirectoryNotFoundException($"Repository not found: {gitDirPath}");
        }
    }
}