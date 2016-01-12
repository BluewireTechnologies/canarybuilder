using System;
using System.IO;
using System.Linq;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Location of a working copy directory.
    /// </summary>
    public class GitWorkingCopy
    {
        public GitWorkingCopy(string workingCopyPath)
        {
            this.Root = workingCopyPath;
        }

        public void CheckExistence()
        {
            if (!Directory.Exists(Root)) throw new DirectoryNotFoundException($"Working copy does not exist: {Root}");
        }

        public string Root { get; }

        // based on __git_ps1 from git_prompt.sh:

        public bool IsRebasing => Directory.Exists(GetDefaultRepository().Path("rebase-apply"))
                                  || Directory.Exists(GetDefaultRepository().Path("rebase-merge"));
        public bool IsMerging => File.Exists(GetDefaultRepository().Path("MERGE_HEAD"));
        public bool IsCherryPicking => File.Exists(GetDefaultRepository().Path("MERGE_HEAD"));
        public bool IsReverting => File.Exists(GetDefaultRepository().Path("REVERT_HEAD"));
        public bool IsBisecting => File.Exists(GetDefaultRepository().Path("BISECT_LOG"));

        /// <summary>
        /// Returns the GitRepository associated with this working copy, based on it's
        /// .git folder or file.
        /// </summary>
        /// <returns></returns>
        public GitRepository GetDefaultRepository()
        {
            var dotGit = Path(".git");
            if (File.Exists(dotGit))
            {
                // Assume .git file inside working copy, pointing to repository directory.
                var relativePath = File.ReadLines(dotGit).FirstOrDefault();
                if (String.IsNullOrWhiteSpace(relativePath))
                {
                    throw new DirectoryNotFoundException($"{dotGit} does not point to the repository.");
                }
                var repoPath = Path(relativePath);
                return new GitRepository(repoPath);
            }

            // Assume .git directory inside working copy.
            return new GitRepository(dotGit);
        }

        public string Path(string relativePath)
        {
            if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
            return System.IO.Path.Combine(Root, relativePath);
        }
    }
}