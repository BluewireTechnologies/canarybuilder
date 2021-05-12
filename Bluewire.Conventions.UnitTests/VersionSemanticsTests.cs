using NUnit.Framework;

namespace Bluewire.Conventions.UnitTests
{
    [TestFixture]
    public class VersionSemanticsTests
    {
        public struct IsAncestorCase
        {
            public string Reference { get; set; }
            public string Subject { get; set; }
            public int? LastSubjectMasterBuild { get; set; }
            public bool Expected { get; set; }

            public override string ToString()
            {
                var description = $"{Subject} ancestor of {Reference} -> {Expected}";
                if (LastSubjectMasterBuild == null) return description;
                return description + $" (Build {LastSubjectMasterBuild} is in master)";
            }
        }

        public static IsAncestorCase[] IsAncestorCases =
        {
            // Release/candidate/etc branches of same major.minor has major.minor.0-beta as an ancestor.
            new IsAncestorCase { Reference = "20.21.10-beta", Subject = "20.21.0-beta", Expected = true },
            new IsAncestorCase { Reference = "20.21.10-rc", Subject = "20.21.0-beta", Expected = true },
            new IsAncestorCase { Reference = "20.21.10-release", Subject = "20.21.0-beta", Expected = true },
            // Same applies to alphas, etc.
            new IsAncestorCase { Reference = "20.21.10-alpha.g35897262", Subject = "20.21.0-beta", Expected = true },
            new IsAncestorCase { Reference = "20.21.10-canary", Subject = "20.21.0-beta", Expected = true },
            // But alphas, etc do not have non-zero builds as ancestors.
            new IsAncestorCase { Reference = "20.21.20-alpha.g35897262", Subject = "20.21.10-beta", Expected = false },
            new IsAncestorCase { Reference = "20.21.20-canary", Subject = "20.21.10-beta", Expected = false },
            // Alphas do have previous major.minor.0-betas as ancestors.
            new IsAncestorCase { Reference = "20.21.20-alpha.g35897262", Subject = "20.20.0-beta", Expected = true },
            new IsAncestorCase { Reference = "20.21.20-canary", Subject = "20.20.0-beta", Expected = true },
            // But not non-zero builds of previous major.minors.
            new IsAncestorCase { Reference = "20.21.20-alpha.g35897262", Subject = "20.20.10-beta", Expected = false },
            new IsAncestorCase { Reference = "20.21.20-canary", Subject = "20.20.10-beta", Expected = false },
            // Unless we know they're in the ancestry of subsequent major.minors.
            new IsAncestorCase { Reference = "20.21.20-alpha.g35897262", Subject = "20.20.10-beta", LastSubjectMasterBuild = 100, Expected = true },
            new IsAncestorCase { Reference = "20.21.20-canary", Subject = "20.20.10-beta", LastSubjectMasterBuild = 100, Expected = true },
            new IsAncestorCase { Reference = "20.21.20-beta", Subject = "20.20.10-beta", LastSubjectMasterBuild = 100, Expected = true },
            new IsAncestorCase { Reference = "20.21.20-rc", Subject = "20.20.10-beta", LastSubjectMasterBuild = 100, Expected = true },
            new IsAncestorCase { Reference = "20.21.20-release", Subject = "20.20.10-beta", LastSubjectMasterBuild = 100, Expected = true },

            // Cannot compare across different tags.
            new IsAncestorCase { Reference = "20.21.20-rc", Subject = "20.21.10-beta", Expected = false },
            new IsAncestorCase { Reference = "20.21.20-release", Subject = "20.21.10-rc", Expected = false },
            new IsAncestorCase { Reference = "20.21.20-release", Subject = "20.21.10-beta", Expected = false },
            new IsAncestorCase { Reference = "20.21.10-rc", Subject = "20.21.20-beta", Expected = false },
            new IsAncestorCase { Reference = "20.21.10-release", Subject = "20.21.20-rc", Expected = false },
            new IsAncestorCase { Reference = "20.21.10-release", Subject = "20.21.20-beta", Expected = false },
            // Unless subject is in the ancestry of subsequent major.minors too.
            new IsAncestorCase { Reference = "20.21.20-rc", Subject = "20.21.10-beta", LastSubjectMasterBuild = 15, Expected = true },
            new IsAncestorCase { Reference = "20.21.20-release", Subject = "20.21.10-beta", LastSubjectMasterBuild = 15, Expected = true },
        };

        [Test]
        public void IsAncestor([ValueSource(nameof(IsAncestorCases))] IsAncestorCase testCase)
        {
            var reference = SemanticVersion.FromString(testCase.Reference);
            var subject = SemanticVersion.FromString(testCase.Subject);
            Assert.That(new VersionSemantics().IsAncestor(reference, subject, testCase.LastSubjectMasterBuild), Is.EqualTo(testCase.Expected));
        }
    }
}
