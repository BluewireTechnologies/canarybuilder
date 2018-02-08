using NUnit.Framework;

namespace Bluewire.Conventions.UnitTests
{
    [TestFixture]
    public class SemanticVersionTests
    {
        public struct Case
        {
            public string Raw { get; set; }
            public SemanticVersion Parsed { get; set; }

            public override string ToString()
            {
                return Raw;
            }
        }

        public static Case[] Cases = {
            new Case { Raw = "17.05.123-beta", Parsed = new SemanticVersion("17","05",123,"beta") },
            new Case { Raw = "17.05.123-rc", Parsed = new SemanticVersion("17","05",123,"rc") },
            new Case { Raw = "17.05.123-canary", Parsed = new SemanticVersion("17","05",123,"canary") },
            new Case { Raw = "17.05.123-release", Parsed = new SemanticVersion("17","05",123,"release") }
        };

        [Test]
        public void CanParse([ValueSource(nameof(Cases))] Case testCase)
        {
            var parsed = SemanticVersion.FromString(testCase.Raw);
            Assert.That(parsed.Major, Is.EqualTo(testCase.Parsed.Major));
            Assert.That(parsed.Minor, Is.EqualTo(testCase.Parsed.Minor));
            Assert.That(parsed.Build, Is.EqualTo(testCase.Parsed.Build));
            Assert.That(parsed.SemanticTag, Is.EqualTo(testCase.Parsed.SemanticTag));
        }

        [Test]
        public void CanFormat([ValueSource(nameof(Cases))] Case testCase)
        {
            Assert.That(testCase.Parsed.ToString(), Is.EqualTo(testCase.Raw));
        }
    }
}
