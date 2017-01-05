using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;

namespace Bluewire.Tools.Runner.FindBuild
{
    public interface IBuildVersionResolutionJob
    {
        Task<string[]> ResolveBuildVersions(GitSession session, Common.GitWrapper.GitRepository repository);
    }
}
