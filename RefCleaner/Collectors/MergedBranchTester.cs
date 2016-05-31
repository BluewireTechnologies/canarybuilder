using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;

namespace RefCleaner.Collectors
{
    public class MergedBranchTester
    {
        private readonly IBranchProvider branchProvider;

        public MergedBranchTester(IBranchProvider branchProvider)
        {
            this.branchProvider = branchProvider;
        }

        /// <summary>
        /// Determine whether the candidate ref is contained in the mergeTarget ref.
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> IsMerged(Ref mergeTarget, Ref candidate)
        {
            var mergedBranches = await branchProvider.GetMergedBranches(mergeTarget);
            return mergedBranches.Contains(candidate);
        }

        public virtual async Task<bool> Exists(Ref mergeTarget)
        {
            return await branchProvider.BranchExists(mergeTarget);
        }
    }
}
