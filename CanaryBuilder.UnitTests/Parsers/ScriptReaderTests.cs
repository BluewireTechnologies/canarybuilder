using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CanaryBuilder.Merge;
using CanaryBuilder.Parsers;
using NUnit.Framework;

namespace CanaryBuilder.UnitTests.Parsers
{
    [TestFixture]
    public class ScriptReaderTests
    {
        [Test]
        public void CommentedLinesAreIgnored()
        {
            var lines = new ScriptReader().EnumerateLines(new StringReader(
@"# First line
line 2
# Third line
"));

            Assert.That(lines.Select(l => l.Content), Is.EqualTo(new[] {
                "line 2",
            }));
        }

        [Test]
        public void LeadingWhitespaceIsPreserved()
        {
            var lines = new ScriptReader().EnumerateLines(new StringReader(
@"      line 1
   line 2
line 3
"));
            Assert.That(lines.Select(l => l.Content), Is.EqualTo(new[] {
                "      line 1",
                "   line 2",
                "line 3"
            }));
        }

        [Test]
        public void TrailingWhitespaceIsRemoved()
        {
            var lines = new ScriptReader().EnumerateLines(new StringReader(
@"line 1             
line 2               
line 3             
"));
            Assert.That(lines.Select(l => l.Content), Is.EqualTo(new[] {
                "line 1",
                "line 2",
                "line 3"
            }));
        }

        [Test]
        public void TrailingCommentsAreIgnored()
        {
            var lines = new ScriptReader().EnumerateLines(new StringReader(
@"line 1  # This is 
line 2  # several lines
line 3# Of comments
"));

            Assert.That(lines.Select(l => l.Content), Is.EqualTo(new[] {
                "line 1",
                "line 2",
                "line 3"
            }));
        }
    }
}
