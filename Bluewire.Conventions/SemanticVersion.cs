using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Bluewire.Conventions
{
    // major.minor.build-semtag
    public class SemanticVersion
    {
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
            var m = Patterns.SemanticVersionStructure.Match(semVer);
            if (!m.Success)
            {
                throw new ArgumentException($"Unable to parse semantic version: {semVer}");
            }
            
            return new SemanticVersion(m.Groups["major"].Value, m.Groups["minor"].Value, int.Parse(m.Groups["build"].Value), m.Groups["semtag"]?.Value);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}.{1}.{2}", Major, Minor, Build);
            if (!string.IsNullOrEmpty(SemanticTag))
                sb.AppendFormat("-{0}", SemanticTag);
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
    }
}
