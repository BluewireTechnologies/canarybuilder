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


        // Assumes we always create a tag when we start a new version
        public string GetVersionZeroBranchName(SemanticVersion semVer)
        {
            return string.Format("{0}.{1}", semVer.Major, semVer.Minor);
        }

        // Assumes sementics defined in BranchType.cs
        public string GetVersionLatestBranchName(SemanticVersion semVer)
        {
            switch (semVer.SemanticTag)
            {
                case "beta": return "master";
                case "rc": return string.Format("candidate/{0}.{1}", semVer.Major, semVer.Minor);
                case "release": return string.Format("release/{0}.{1}", semVer.Major, semVer.Minor);
                case "canary": return null;
                default: throw new InvalidOperationException("Unknown semantic tag value");
            }
        }
    }
}
