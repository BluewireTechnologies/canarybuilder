using System.IO;
using CanaryBuilder.Common.Git;

namespace CanaryBuilder.Merge
{
    public interface IWorkingCopyVerifier
    {
        bool Verify(GitWorkingCopy workingCopy, TextWriter details);
    }
}