using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;

namespace RefCleaner.Collectors
{
    public class CachingBranchProvider : IBranchProvider
    {
        private readonly IBranchProvider branchProvider;
        private readonly Dictionary<Ref, ICollection<Ref>> mergeCache = new Dictionary<Ref, ICollection<Ref>>();
        private readonly Dictionary<Ref, bool> existsCache = new Dictionary<Ref, bool>();

        public CachingBranchProvider(IBranchProvider branchProvider)
        {
            this.branchProvider = branchProvider;
        }

        public Task<BranchDetails[]> GetAllBranches()
        {
            // No point caching this.
            return branchProvider.GetAllBranches();
        }

        public async Task<ICollection<Ref>> GetMergedBranches(Ref mergeTarget)
        {
            return await GetOrAdd<ICollection<Ref>>(mergeCache, mergeTarget, async () => new HashSet<Ref>(await branchProvider.GetMergedBranches(mergeTarget)));
        }

        public async Task<bool> BranchExists(Ref branch)
        {
            return await GetOrAdd(existsCache, branch, async () => await branchProvider.BranchExists(branch));
        }

        private async Task<T> GetOrAdd<T>(Dictionary<Ref, T> cache, Ref subject, Func<Task<T>> fetch)
        {
            T result;
            lock(cache)
            {
                if (cache.TryGetValue(subject, out result)) return result;
            }
            var fetched = await fetch();
            lock(cache)
            {
                if (cache.TryGetValue(subject, out result)) return result;
                cache[subject] = fetched;
                return fetched;
            }
        }
    }
}
