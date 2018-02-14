using System.IO;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Common.GitWrapper.Parsing;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.UnitTests.Parsing
{
    [TestFixture]
    public class GitStatusParserTests
    {
        public struct Case
        {
            public string Line { get; set; }
            public GitStatusEntry Expected { get; set; }

            public override string ToString()
            {
                return Line;
            }
        }

        public static Case[] Cases = {
            new Case { Line = " M some/path/in/repo", Expected = new GitStatusEntry { Path = "some/path/in/repo", IndexState = IndexState.Unmodified, WorkTreeState = WorkTreeState.Modified } },
            new Case { Line = "M  some/path/in/repo", Expected = new GitStatusEntry { Path = "some/path/in/repo", IndexState = IndexState.Modified, WorkTreeState = WorkTreeState.Unmodified } },
            new Case { Line = "MD \"some/path with whitespace/in/repo\"", Expected = new GitStatusEntry { Path = "some/path with whitespace/in/repo", IndexState = IndexState.Modified, WorkTreeState = WorkTreeState.Deleted } },
            new Case { Line = "C  original/path -> copy/path", Expected = new GitStatusEntry { Path = "original/path", NewPath = "copy/path", IndexState = IndexState.Copied, WorkTreeState = WorkTreeState.Unmodified } },
            new Case { Line = "A  \"path/with/some\\\"quotes\"", Expected = new GitStatusEntry { Path = "path/with/some\"quotes", IndexState = IndexState.Added, WorkTreeState = WorkTreeState.Unmodified } },

            // Edge cases, maybe impossible on Windows?
            new Case { Line = "C  a->b -> a->c", Expected = new GitStatusEntry { Path = "a->b", NewPath = "a->c", IndexState = IndexState.Copied, WorkTreeState = WorkTreeState.Unmodified } },
            new Case { Line = "R  \"a -> b\" -> a->c", Expected = new GitStatusEntry { Path = "a -> b", NewPath = "a->c", IndexState = IndexState.Renamed, WorkTreeState = WorkTreeState.Unmodified } },
            new Case { Line = "R  \"a\\\\nb\" -> a->c", Expected = new GitStatusEntry { Path = "a\\nb", NewPath = "a->c", IndexState = IndexState.Renamed, WorkTreeState = WorkTreeState.Unmodified } }
        };

        [Test]
        public void CanParseStatusLine([ValueSource(nameof(Cases))] Case testCase)
        {
            var parser = new GitStatusParser();
            GitStatusEntry parsed;
            var result = parser.Parse(testCase.Line, out parsed);

            if (!result)
            {
                var writer = new StringWriter();
                foreach (var error in parser.Errors) error.Explain(writer);
                Assert.Fail(writer.ToString());
            }

            Assert.That(parsed.Path, Is.EqualTo(testCase.Expected.Path));
            Assert.That(parsed.NewPath, Is.EqualTo(testCase.Expected.NewPath));
            Assert.That(parsed.IndexState, Is.EqualTo(testCase.Expected.IndexState));
            Assert.That(parsed.WorkTreeState, Is.EqualTo(testCase.Expected.WorkTreeState));
        }
    }
}
