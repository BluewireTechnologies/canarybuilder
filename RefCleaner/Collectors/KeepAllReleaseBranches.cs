using System;
using System.Threading.Tasks;
using Bluewire.Conventions;
using System.Linq;

namespace RefCleaner.Collectors
{
    public class KeepAllReleaseBranches : IRefFilter
    {
        public Task ApplyFilter(BranchDetails details)
        {
            ApplyFilterSync(details);
            return Task.CompletedTask;
        }

        public void ApplyFilterSync(BranchDetails details)
        {
            if (details.Name.Equals("master", StringComparison.OrdinalIgnoreCase))
            {
                details.UpdateDisposition(BranchDisposition.MustKeep);
                return;
            }
            StructuredBranch structured;
            // If we can't parse the branch name, leave it alone.
            if (!StructuredBranch.TryParse(details.Name, out structured)) return;
            if (structured.Namespace == null) return;

            if (structured.Namespace.Split('/').Contains("release", StringComparer.OrdinalIgnoreCase))
            {
                details.UpdateDisposition(BranchDisposition.MustKeep);
            }
            if (structured.Namespace.Split('/').Contains("candidate", StringComparer.OrdinalIgnoreCase))
            {
                details.UpdateDisposition(BranchDisposition.MustKeep);
            }
        }
    }
}
