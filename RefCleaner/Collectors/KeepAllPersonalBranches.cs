using System;
using System.Threading.Tasks;
using Bluewire.Conventions;
using System.Linq;

namespace RefCleaner.Collectors
{
    public class KeepAllPersonalBranches : IRefFilter
    {
        public Task ApplyFilter(BranchDetails details)
        {
            ApplyFilterSync(details);
            return Task.CompletedTask;
        }

        public void ApplyFilterSync(BranchDetails details)
        {
            StructuredBranch structured;
            // If we can't parse the branch name, leave it alone.
            if (!StructuredBranch.TryParse(details.Name, out structured)) return;
            if (structured.Namespace == null) return;

            if (structured.Namespace.Split('/').Contains("personal", StringComparer.OrdinalIgnoreCase))
            {
                details.UpdateDisposition(BranchDisposition.MustKeep);
            }
        }
    }
}
