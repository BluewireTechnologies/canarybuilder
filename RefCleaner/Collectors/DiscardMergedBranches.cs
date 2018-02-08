using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;

namespace RefCleaner.Collectors
{
    public class DiscardMergedBranches : IRefFilter
    {
        private readonly MergedBranchTester branchTester;

        public DiscardMergedBranches(MergedBranchTester branchTester)
        {
            this.branchTester = branchTester;
        }

        public async Task ApplyFilter(BranchDetails details)
        {
            StructuredBranch structured;
            // If we can't parse the branch name, leave it alone.
            if (!StructuredBranch.TryParse(details.Name, out structured)) return;

            var mergeTarget = new Ref(structured.TargetRelease != null ? $"release/{structured.TargetRelease}" : "master");
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
