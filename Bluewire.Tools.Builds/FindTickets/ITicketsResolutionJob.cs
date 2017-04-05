using Bluewire.Common.GitWrapper;
using System.Threading.Tasks;

namespace Bluewire.Tools.Builds.FindTickets
{
    public interface ITicketsResolutionJob
    {
        Task<string[]> ResolveTickets(GitSession session, Common.GitWrapper.GitRepository repository);
    }
}
