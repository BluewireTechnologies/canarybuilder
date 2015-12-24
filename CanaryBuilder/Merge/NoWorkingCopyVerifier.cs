using System.IO;
using Bluewire.Common.Git;

namespace CanaryBuilder.Merge
{
    public class NoWorkingCopyVerifier : IWorkingCopyVerifier
    {
        public bool Verify(GitWorkingCopy workingCopy, TextWriter details)
        {
            return true;
        }
    }
}