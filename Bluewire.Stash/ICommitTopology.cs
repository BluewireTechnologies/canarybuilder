using System.Collections.Generic;
using System.Threading.Tasks;
using Bluewire.Conventions;

namespace Bluewire.Stash
{
    public interface ICommitTopology
    {
        /// <summary>
        /// Resolves a semantic version to a commit hash, or a commit hash to a semantic version, to
        /// fill in the missing parts of 'marker'.
        /// </summary>
        Task<ResolvedVersionMarker?> FullyResolve(VersionMarker marker);
        /// <summary>
        /// Returns true if 'subject' is definitely in the ancestry of 'reference'.
        /// </summary>
        Task<bool> IsAncestor(ResolvedVersionMarker reference, ResolvedVersionMarker subject);
        /// <summary>
        /// Enumerates the ancestry chain of 'marker' within the same major.minor.
        /// </summary>
        IAsyncEnumerable<ResolvedVersionMarker> EnumerateAncestry(VersionMarker marker);
        /// <summary>
        /// Return info about the commit immediately prior to 'marker's major.minor.0.
        /// </summary>
        Task<ResolvedVersionMarker?> GetLastVersionInMajorMinor(SemanticVersion semVer);
    }
}
