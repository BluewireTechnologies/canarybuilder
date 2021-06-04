using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Conventions;

namespace Bluewire.Stash
{
    public interface ILocalStashRepository : IStashRepository
    {
        /// <summary>
        /// Find a stash which matches the specified commit/version or is a close ancestor of it, without knowledge of Git repository topology.
        /// </summary>
        Task<IStash?> FindClosestAncestor(VersionMarker marker);
        /// <summary>
        /// Find a stash which matches the specified commit/version or is a close ancestor of it, using knowledge of Git repository topology
        /// to find the best match.
        /// </summary>
        Task<IStash?> FindClosestAncestor(ICommitTopology topology, VersionMarker marker);
        /// <summary>
        /// Get the stash exactly matching the specified commit/version, or create it if missing.
        /// </summary>
        Task<IStash> GetOrCreateExact(VersionMarker marker);
        /// <summary>
        /// Get the stash matching the specified commit/version, or create it if missing.
        /// </summary>
        Task<IStash> GetOrCreate(VersionMarker marker);
        /// <summary>
        /// Get an existing stash matching the specified commit/version, or null if none was found.
        /// </summary>
        Task<IStash?> TryGet(VersionMarker marker);
        /// <summary>
        /// Delete the stash which exactly matches the specified version.
        /// </summary>
        Task Delete(VersionMarker marker);

        IAsyncEnumerable<string> CleanUpTemporaryObjects(CancellationToken token);
    }
}
