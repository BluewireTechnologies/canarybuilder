using System.IO;
using Bluewire.Common.GitWrapper;

namespace CanaryBuilder.Merge
{
    public interface IWorkingCopyVerifier
    {
        bool Verify(GitWorkingCopy workingCopy, TextWriter details);
    }
}