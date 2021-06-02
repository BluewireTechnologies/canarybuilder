using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace Bluewire.Tools.Builds.FindBuild
{
    public interface IBuildVersionResolutionJob
    {
        Task<SemanticVersion[]> ResolveBuildVersions(GitSession session, IGitFilesystemContext workingCopyOrRepo);
    }
}
