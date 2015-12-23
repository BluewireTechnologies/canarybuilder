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

        public string Root { get; }
    }
}