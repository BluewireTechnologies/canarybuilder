using System;
using System.IO;

namespace Bluewire.Common.Git
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

        public string Path(string relativePath)
        {
            if (relativePath == null) throw new ArgumentNullException(nameof(relativePath));
            return System.IO.Path.Combine(Root, relativePath);
        }
    }
}