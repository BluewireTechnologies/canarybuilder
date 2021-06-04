using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Conventions;

namespace Bluewire.Stash.Remote
{
    public class CachedStashRepository : IStashRepository
    {
        private readonly List<VersionMarker> list;

        private CachedStashRepository(List<VersionMarker> list)
        {
            this.list = list;
        }

        public static async Task<CachedStashRepository> Preload(IRemoteStashRepository repository, CancellationToken token)
        {
            var list = new List<VersionMarker>();
            await foreach (var marker in repository.List(token))
            {
                list.Add(marker);
            }
            return new CachedStashRepository(list);
        }

        public async IAsyncEnumerable<VersionMarker> List()
        {
            foreach (var marker in list) yield return marker;
        }

        public async Task<VersionMarker[]> List(SemanticVersion majorMinor)
        {
            return list.Where(k => k.SemanticVersion?.Major == majorMinor.Major && k.SemanticVersion?.Minor == majorMinor.Minor).ToArray();
        }

        public async Task<VersionMarker?> FindClosestAncestor(VersionMarker marker)
        {
            var resolver = new StashResolver(this);
            var stashMarker = await resolver.FindClosestAncestor(marker);
            if (!stashMarker.IsValid) return null;
            return stashMarker;
        }

        public async Task<VersionMarker?> FindClosestAncestor(ICommitTopology topology, VersionMarker marker)
        {
            var resolver = new StashResolver(this);
            var resolved = await topology.FullyResolve(marker);
            if (resolved == null) return null;  // Commit cannot be resolved?

            var stashMarker = await resolver.FindClosestAncestor(topology, resolved.Value);
            if (!stashMarker.IsValid) return null;
            return stashMarker;
        }
    }
}
