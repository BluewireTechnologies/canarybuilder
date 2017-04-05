using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Tools.Builds.Shared;

namespace Bluewire.Tools.Builds.FindCommits
{
    public interface IBuildVersionResolutionJob
    {
        Task<Build[]> ResolveCommits(GitSession session, Common.GitWrapper.GitRepository repository);
    }
}
