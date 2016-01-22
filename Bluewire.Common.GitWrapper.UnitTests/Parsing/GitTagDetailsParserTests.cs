using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Common.GitWrapper.Parsing;
using NUnit.Framework;

namespace Bluewire.Common.GitWrapper.UnitTests.Parsing
{
    [TestFixture]
    public class GitTagDetailsParserTests
    {
        [Test]
        public void ParsesTagDetailsStream()
        {
            var lines = new[] {
"object ec732688790491cfafd45fa792005dd3deb7ff84",
"type commit",
"tag maint/15.14",
"tagger Alex Davidson <alex.davidson@bluewire-technologies.com> 1449670186 +0000",
"",
"Tag title",
"",
"Message body",
"with multiple lines",
"",
"and whitespace"
            };

            var details = new GitTagDetailsParser().Parse(lines);
            Assert.That(details.Name, Is.EqualTo("maint/15.14"));
            Assert.That(details.Ref, Is.EqualTo(new Ref("refs/tags/maint/15.14")));
            Assert.That(details.ResolvedRef, Is.EqualTo(new Ref("ec732688790491cfafd45fa792005dd3deb7ff84")));
            Assert.That(details.Message, Is.EqualTo(@"Tag title

Message body
with multiple lines

and whitespace
"));
        }
        /*


        */
    }
}
