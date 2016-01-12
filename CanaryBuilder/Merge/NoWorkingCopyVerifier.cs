using System.IO;
using Bluewire.Common.GitWrapper;

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