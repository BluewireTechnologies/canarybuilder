using System;
using System.IO;

namespace Bluewire.Common.GitWrapper
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
        
        public string Path(string relativePath)
        {
            if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
            return System.IO.Path.Combine(gitDirPath, relativePath);
        }
    }
}
