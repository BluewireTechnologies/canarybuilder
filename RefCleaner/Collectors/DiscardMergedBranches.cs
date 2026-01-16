using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace RefCleaner.Collectors
{
    public class DiscardMergedBranches : IRefFilter
    {
        private readonly MergedBranchTester branchTester;
        private readonly bool aggressive;

        public DiscardMergedBranches(MergedBranchTester branchTester, bool aggressive)
        {
            this.branchTester = branchTester;
            this.aggressive = aggressive;
        }

        public async Task ApplyFilter(BranchDetails details)
        {
            StructuredBranch structured;
            // If we can't parse the branch name, leave it alone.
            if (!StructuredBranch.TryParse(details.Name, out structured)) return;

            var versionedTarget = aggressive ? $"backport/{structured.TargetRelease}" : $"release/{structured.TargetRelease}";
            var mergeTarget = new Ref(structured.TargetRelease != null ? versionedTarget : "master");
            if (await branchTester.Exists(mergeTarget))
            {
                if (await branchTester.IsMerged(mergeTarget, details.Ref))
                {
                    details.UpdateDisposition(BranchDisposition.Discard);
                }
            }
        }
    }
}
