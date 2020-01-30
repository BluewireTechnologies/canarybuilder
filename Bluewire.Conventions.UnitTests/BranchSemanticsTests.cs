using System;
using NUnit.Framework;

namespace Bluewire.Conventions.UnitTests
{
    [TestFixture]
    public class BranchSemanticsTests
    {
        public struct Case
        {
            public string Raw { get; set; }
            public SemanticVersion Parsed { get; set; }
            public string BranchStart { get; set; }
            public string[] BranchEnds { get; set; }

            public override string ToString()
            {
                return Raw;
            }
        }

        public static Case[] Cases = {
            new Case { Raw = "17.05.123-beta", Parsed = new SemanticVersion("17","05",123,"beta"), BranchStart = "17.05", BranchEnds = new[] { "backport/17.05", "master" } },
            new Case { Raw = "17.05.123-rc", Parsed = new SemanticVersion("17","05",123,"rc"), BranchStart = "17.05", BranchEnds = new[] { "candidate/17.05" } },
            new Case { Raw = "17.05.123-canary", Parsed = new SemanticVersion("17","05",123,"canary"), BranchStart = "17.05", BranchEnds = new string[0] },
            new Case { Raw = "17.05.123-release", Parsed = new SemanticVersion("17","05",123,"release"), BranchStart = "17.05", BranchEnds = new[] { "release/17.05" } }
        };

        [Test]
        public void CanIdentifyStartOfBranch([ValueSource(nameof(Cases))] Case testCase)
        {
            var sut = new BranchSemantics();
            var parsed = SemanticVersion.FromString(testCase.Raw);
            Assert.That(sut.GetVersionZeroBranchName(parsed), Is.EqualTo(testCase.BranchStart));
            Assert.That(sut.GetVersionLatestBranchNames(parsed), Is.EqualTo(testCase.BranchEnds));
        }

        [Test]
        public void ThrowsExceptionIfSemTagIsMissing()
        {
            var sut = new BranchSemantics();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var semVer = new SemanticVersion("17", "05", 123, null);
                sut.GetVersionLatestBranchNames(semVer);
            });
        }

        [Test]
        public void ThrowsExceptionIfSemTagIsEmpty()
        {
            var sut = new BranchSemantics();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var semVer = new SemanticVersion("17", "05", 123, "");
                sut.GetVersionLatestBranchNames(semVer);
            });
        }

        [Test]
        public void ThrowsExceptionIfSemTagIsUnknown()
        {
            var sut = new BranchSemantics();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var semVer = new SemanticVersion("17", "05", 123, "unknownsemtagfortesting");
                sut.GetVersionLatestBranchNames(semVer);
            });
        }
    }
}
