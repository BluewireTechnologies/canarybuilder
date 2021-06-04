using System.Collections.Generic;
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
        /// <summary>
        /// List all commits/versions for which stashes exist, in the same major.minor as the specified SemanticVersion.
        /// </summary>
        Task<VersionMarker[]> List(SemanticVersion majorMinor);
    }
}
