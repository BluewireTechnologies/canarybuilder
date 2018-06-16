using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;

namespace RefCleaner.Collectors
{
    public class FilteredBranchCollector : IRefCollector
    {
        private readonly IBranchProvider branchProvider;
        private readonly IRefFilter[] filters;

        public FilteredBranchCollector(IBranchProvider branchProvider, params IRefFilter[] filters)
        {
            this.branchProvider = branchProvider;
            this.filters = filters;
        }

        public async Task<IEnumerable<Ref>> CollectRefs()
        {
            var branches = await branchProvider.GetAllBranches();
            foreach (var branch in branches)
            {
                await ApplyFilters(branch);
            }

            return branches.Where(b => b.Disposition == BranchDisposition.Discard)
                .Select(b => RefHelper.PutInHierarchy("heads", b.Ref))
                .ToArray();
        }

        private async Task ApplyFilters(BranchDetails branch)
        {
            foreach (var filter in filters)
            {
                await filter.ApplyFilter(branch);
            }
        }
    }
}
