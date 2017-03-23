using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Tools.Runner.Shared;

namespace Bluewire.Tools.Runner.FindCommits
{
    public interface IBuildVersionResolutionJob
    {
        Task<Build[]> ResolveCommits(GitSession session, Common.GitWrapper.GitRepository repository);
    }
}
