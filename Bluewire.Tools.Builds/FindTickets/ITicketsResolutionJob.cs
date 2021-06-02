using Bluewire.Common.GitWrapper;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.Builds.FindTickets
{
    public interface ITicketsResolutionJob
    {
        Task<string[]> ResolveTickets(GitSession session, IGitFilesystemContext workingCopyOrRepo);
    }
}
