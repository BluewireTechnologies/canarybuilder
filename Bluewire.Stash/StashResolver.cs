using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Conventions;

namespace Bluewire.Stash
{
    /// <summary>
    /// Topological resolver which requires only lists of available stashes.
    /// </summary>
    public class StashResolver
    {
        private readonly IStashRepository repository;

        public StashResolver(IStashRepository repository)
        {
            this.repository = repository;
        }

        public async Task<VersionMarker> FindClosestAncestor(VersionMarker marker)
        {
            if (marker.SemanticVersion == null) throw new ArgumentException("Cannot resolve ancestry of commit hashes without a source of commit topology info.");

            await foreach (var candidate in EnumerateProbableAncestorsByCanonicalVersionNumber(null, marker.SemanticVersion))
            {
                return candidate;
            }
            return default;
        }

        public async Task<VersionMarker> FindClosestAncestor(ICommitTopology topology, ResolvedVersionMarker marker)
        {
            var candidates = await repository.List(marker.SemanticVersion);

            if (candidates.Any())
            {
                // Search commits within the same major.minor for a perfect match.
                var byHash = candidates
                    .Where(c => c.CommitHash != null)
                    .ToDictionary(c => c.CommitHash!, StringComparer.OrdinalIgnoreCase);
                await foreach (var commit in topology.EnumerateAncestry(marker))
                {
                    if (byHash.TryGetValue(commit.CommitHash, out var match))
                    {
                        return match;
                    }
                }

                var withVersionNumber = candidates
                    .Where(c => c.SemanticVersion != null)
                    .OrderByDescending(c => c.SemanticVersion!.Build)
                    .ToArray();
                // Fall back to matching by version number.
                await foreach (var commit in topology.EnumerateAncestry(marker))
                {
                    var matches = withVersionNumber
                        .Where(c => new VersionSemantics().IsAncestor(commit.SemanticVersion, c.SemanticVersion))
                        .Take(1)
                        .ToArray();

                    if (matches.Any()) return matches.First();
                }
            }

            await foreach (var candidate in EnumerateProbableAncestorsByCanonicalVersionNumber(topology, marker.SemanticVersion))
            {
                var resolved = await topology.FullyResolve(candidate);
                if (resolved == null) continue;
                if (await topology.IsAncestor(marker, resolved.Value)) return resolved.Value;
            }

            return default;
        }

        /// <summary>
        /// Enumerate probable ancestors of 'markerVersion' based on version number heuristics.
        /// </summary>
        private async IAsyncEnumerable<VersionMarker> EnumerateProbableAncestorsByCanonicalVersionNumber(ICommitTopology? topology, SemanticVersion markerVersion)
        {
            if (markerVersion == null) throw new ArgumentNullException(nameof(markerVersion));
            await foreach (var entry in repository.List())
            {
                if (entry.SemanticVersion == null) continue;
                var startOfMajorMinor = topology == null ? null : await topology.GetLastVersionInMajorMinor(entry.SemanticVersion);
                if (new VersionSemantics().IsAncestor(markerVersion, entry.SemanticVersion, startOfMajorMinor?.SemanticVersion.Build))
                {
                    yield return entry;
                }
            }
        }
    }
}
