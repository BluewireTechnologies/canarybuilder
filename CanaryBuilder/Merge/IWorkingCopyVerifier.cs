using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using CanaryBuilder.Logging;

namespace CanaryBuilder.Merge
{
    public interface IWorkingCopyVerifier
    {
        Task Verify(GitWorkingCopy workingCopy, IJobLogger details);
    }
}
