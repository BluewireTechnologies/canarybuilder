using System;
using System.Threading.Tasks;

namespace RefCleaner.Collectors
{
    public class KeepRecentBranches : IRefFilter
    {
        private readonly DateTimeOffset cutoffDate;

        public KeepRecentBranches(DateTimeOffset cutoffDate)
        {
            this.cutoffDate = cutoffDate;
        }

        public Task ApplyFilter(BranchDetails details)
        {
            if(details.CommitDatestamp >= cutoffDate)
            {
                details.UpdateDisposition(BranchDisposition.MustKeep);
            }
            return Task.CompletedTask;
        }
    }
}
