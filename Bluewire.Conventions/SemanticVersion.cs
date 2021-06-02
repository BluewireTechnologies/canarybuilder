using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Bluewire.Conventions
{
    // major.minor.build-semtag
    public class SemanticVersion
    {
        private sealed class MajorMinorBuildRelationalComparer : IComparer<SemanticVersion>
        {
            public int Compare(SemanticVersion x, SemanticVersion y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                var majorComparison = string.Compare(x.Major, y.Major, StringComparison.OrdinalIgnoreCase);
                if (majorComparison != 0) return majorComparison;
                var minorComparison = string.Compare(x.Minor, y.Minor, StringComparison.OrdinalIgnoreCase);
                if (minorComparison != 0) return minorComparison;
                return x.Build.CompareTo(y.Build);
            }
        }

        public static IComparer<SemanticVersion> MajorMinorBuildComparer { get; } = new MajorMinorBuildRelationalComparer();

        private sealed class SemanticVersionEqualityComparer : IEqualityComparer<SemanticVersion>
        {
            public bool Equals(SemanticVersion x, SemanticVersion y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.Major, y.Major, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(x.Minor, y.Minor, StringComparison.OrdinalIgnoreCase) &&
                       x.Build == y.Build &&
                       string.Equals(x.SemanticTag, y.SemanticTag, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(SemanticVersion obj)
            {
                unchecked
                {
                    var hashCode = (obj.Major != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Major) : 0);
                    hashCode = (hashCode * 397) ^ (obj.Minor != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Minor) : 0);
                    hashCode = (hashCode * 397) ^ obj.Build;
                    hashCode = (hashCode * 397) ^ (obj.SemanticTag != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.SemanticTag) : 0);
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<SemanticVersion> EqualityComparer { get; } = new SemanticVersionEqualityComparer();

        public readonly static string[] KnownSemanticTags = new string[4] { "beta", "rc", "release", "canary" };

        public bool IsComplete
        {
            get
            {
                return !string.IsNullOrWhiteSpace(SemanticTag) && KnownSemanticTags.Contains(SemanticTag);
            }
        }

        public SemanticVersion(string major, string minor, int build, string semanticTag)
        {
            Major = major;
            Minor = minor;
            Build = build;
            SemanticTag = semanticTag;
        }

        public SemanticVersion(string versionNumber, int buildNumber, BranchType branchType)
        {
            var majorMinor = versionNumber.Split('.');
            Major = majorMinor[0];
            Minor = majorMinor[1];
            Build = buildNumber;
            SemanticTag = branchType.SemanticTag;
        }

        public static SemanticVersion FromString(string semVer)
        {
            if (TryParse(semVer, out var semanticVersion)) return semanticVersion;
            throw new ArgumentException($"Unable to parse semantic version: {semVer}");
        }

        public static bool TryParse(string semVer, out SemanticVersion semanticVersion)
        {
            var m = Patterns.SemanticVersionStructure.Match(semVer);
            if (!m.Success)
            {
                semanticVersion = null;
                return false;
            }

            semanticVersion = new SemanticVersion(m.Groups["major"].Value, m.Groups["minor"].Value, int.Parse(m.Groups["build"].Value), m.Groups["semtag"]?.Value);
            return true;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}.{1}.{2}", Major, Minor, Build);
            if (!string.IsNullOrEmpty(SemanticTag)) sb.AppendFormat("-{0}", SemanticTag);
            return sb.ToString();
        }

        public string Major { get; }
        public string Minor { get; }
        public int Build { get; }
        public string SemanticTag { get; }

        public static SemanticVersion FindEarliestSemanticTag(SemanticVersion[] originalVersions)
        {
            if (originalVersions.Length <= 0) throw new ArgumentException("No versions supplied.");

            var betaVersion = originalVersions.Where(v => v.SemanticTag == "beta").SingleOrDefault();
            if (betaVersion != null) return betaVersion;

            var rcVersion = originalVersions.Where(v => v.SemanticTag == "rc").SingleOrDefault();
            if (rcVersion != null) return rcVersion;

            var releaseVersion = originalVersions.Where(v => v.SemanticTag == "release").SingleOrDefault();
            if (releaseVersion != null) return releaseVersion;

            throw new ArgumentException("No versions with a valid semantic tag supplied.");
        }

        public SemanticVersion WithTag(string tag)
        {
            return new SemanticVersion(Major, Minor, Build, tag);
        }
    }
}
