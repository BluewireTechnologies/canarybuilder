using System;
using System.Collections.Generic;
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
                case "backport": return BranchType.Beta;
                case "candidate": return BranchType.ReleaseCandidate;
                case "release": return BranchType.Release;
                case "canary": return BranchType.Canary;
            }
            return BranchType.None;
        }

        public string[] GetBranchFilters(params BranchType[] types)
        {
            return types.Distinct().SelectMany(t => t.BranchFilters).Where(b => !String.IsNullOrWhiteSpace(b)).ToArray();
        }

        public string[] GetRemoteBranchFilters(params BranchType[] types)
        {
            return types.Distinct().SelectMany(t => t.BranchFilters).Where(b => !String.IsNullOrWhiteSpace(b)).Select(b => $"*/{b}").ToArray();
        }


        // Assumes we always create a tag when we start a new version
        public string GetVersionZeroBranchName(SemanticVersion semVer)
        {
            return string.Format("{0}.{1}", semVer.Major, semVer.Minor);
        }

        public string[] GetVersionLatestBranchNames(SemanticVersion semVer) => GetVersionLatestBranchNamesInternal(semVer).ToArray();

        // Assumes sementics defined in BranchType.cs
        private IEnumerable<string> GetVersionLatestBranchNamesInternal(SemanticVersion semVer)
        {
            if (string.IsNullOrEmpty(semVer.SemanticTag)) throw new ArgumentException($"No semantic tag specified: {semVer}");
            switch (semVer.SemanticTag)
            {
                case "beta":
                    yield return $"backport/{semVer.Major}.{semVer.Minor}";
                    yield return "master";
                    break;
                case "rc":
                    yield return $"candidate/{semVer.Major}.{semVer.Minor}";
                    break;
                case "release":
                    yield return $"release/{semVer.Major}.{semVer.Minor}";
                    break;
                case "canary":
                    break;
                default: throw new InvalidOperationException("Unknown semantic tag value");
            }
        }
    }
}
