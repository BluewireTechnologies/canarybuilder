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
            public string BranchEnd { get; set; }

            public override string ToString()
            {
                return Raw;
            }
        }

        public static Case[] Cases = {
            new Case { Raw = "17.05.123-beta", Parsed = new SemanticVersion("17","05",123,"beta"), BranchStart = "17.05", BranchEnd = "master" },
            new Case { Raw = "17.05.123-rc", Parsed = new SemanticVersion("17","05",123,"rc"), BranchStart = "17.05", BranchEnd = "candidate/17.05" },
            new Case { Raw = "17.05.123-canary", Parsed = new SemanticVersion("17","05",123,"canary"), BranchStart = "17.05", BranchEnd = null },
            new Case { Raw = "17.05.123-release", Parsed = new SemanticVersion("17","05",123,"release"), BranchStart = "17.05", BranchEnd = "release/17.05" }
        };

        [Test]
        public void CanIdentifyStartOfBranch([ValueSource(nameof(Cases))] Case testCase)
        {
            var sut = new BranchSemantics();
            var parsed = SemanticVersion.FromString(testCase.Raw);
            Assert.That(sut.GetVersionZeroBranchName(parsed), Is.EqualTo(testCase.BranchStart));
            Assert.That(sut.GetVersionLatestBranchName(parsed), Is.EqualTo(testCase.BranchEnd));
        }

        [Test]
        public void ThrowsExceptionIfSemTagIsMissing()
        {
            var sut = new BranchSemantics();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var semVer = new SemanticVersion("17", "05", 123, null);
                sut.GetVersionLatestBranchName(semVer);
            });
        }

        [Test]
        public void ThrowsExceptionIfSemTagIsEmpty()
        {
            var sut = new BranchSemantics();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var semVer = new SemanticVersion("17", "05", 123, "");
                sut.GetVersionLatestBranchName(semVer);
            });
        }

        [Test]
        public void ThrowsExceptionIfSemTagIsUnknown()
        {
            var sut = new BranchSemantics();

            Assert.Throws<InvalidOperationException>(() =>
            {
                var semVer = new SemanticVersion("17", "05", 123, "unknownsemtagfortesting");
                sut.GetVersionLatestBranchName(semVer);
            });
        }
    }
}
