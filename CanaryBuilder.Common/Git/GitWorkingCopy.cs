namespace CanaryBuilder.Common.Git
{
    /// <summary>
    /// Location of a working copy directory.
    /// </summary>
    public class GitWorkingCopy
    {
        private readonly string workingCopyPath;

        public GitWorkingCopy(string workingCopyPath)
        {
            this.workingCopyPath = workingCopyPath;
        }
    }
}