using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Conventions;

namespace Bluewire.Stash
{
    public interface IStashRepository
    {
        /// <summary>
        /// List all commits/versions for which stashes exist, in descending order of version (major.minor.build).
        /// </summary>
        IAsyncEnumerable<VersionMarker> List();
    }

    public static class StashRepositoryExtensions
    {
        /// <summary>
        /// List all commits/versions for which stashes exist, in the same or earlier major.minor as the specified SemanticVersion.
        /// </summary>
        public static async Task<VersionMarker[]> ListPossibleAncestors(this IStashRepository repository, SemanticVersion majorMinor)
        {
            var list = new List<VersionMarker>();
            await foreach (var version in repository.List())
            {
                if (version.SemanticVersion == null) continue;
                if (SemanticVersion.MajorMinorBuildComparer.Compare(version.SemanticVersion, majorMinor) > 0) continue;
                list.Add(version);
            }
            return list.ToArray();
        }
    }
}
