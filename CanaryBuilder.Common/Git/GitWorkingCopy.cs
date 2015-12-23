using System.IO;

namespace CanaryBuilder.Common.Git
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
    }
}