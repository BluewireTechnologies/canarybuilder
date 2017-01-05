using System;
using System.Linq;

namespace Bluewire.Conventions
{
    public class BranchSemantics
    {
        public BranchType GetBranchType(StructuredBranch branch)
        {
            if (branch.Name == "master") return BranchType.Beta;

            var lastNamespacePart = branch.Namespace?.Substring(branch.Namespace.LastIndexOf('/') + 1);
            if (lastNamespacePart == null) return BranchType.None;

            switch (lastNamespacePart)
            {
                case "candidate": return BranchType.ReleaseCandidate;
                case "release": return BranchType.Release;
                case "canary": return BranchType.Canary;
            }
            return BranchType.None;
        }

        public string[] GetBranchFilters(params BranchType[] types)
        {
            return types.Distinct().Select(t => t.BranchFilter).Where(b => !String.IsNullOrWhiteSpace(b)).ToArray();
        }

        public string[] GetRemoteBranchFilters(params BranchType[] types)
        {
            return types.Distinct().Select(t => t.BranchFilter).Where(b => !String.IsNullOrWhiteSpace(b)).Select(b => $"*/{b}").ToArray();
        }
    }
}
