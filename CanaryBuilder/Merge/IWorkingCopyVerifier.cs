using System.IO;
using Bluewire.Common.Git;

namespace CanaryBuilder.Merge
{
    public interface IWorkingCopyVerifier
    {
        bool Verify(GitWorkingCopy workingCopy, TextWriter details);
    }
}