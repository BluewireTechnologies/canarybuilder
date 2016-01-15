using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bluewire.Conventions.UnitTests
{
    [TestFixture]
    public class StructuredBranchTests
    {
        public struct Case
        {
            public string Raw { get; set; }
            public StructuredBranch Parsed { get; set; }
            
            public override string ToString()
            {
                return Raw;
            }
        }

        /// <summary>
        /// Test cases defined on the Wiki.
        /// 
        /// wiki:DevelopmentWorkflow/BranchNames
        /// </summary>
        public static Case[] Cases = {
            new Case { Raw = "spike/foo-might-be-a-cool-thing", Parsed = new StructuredBranch { Namespace = "spike", Name = "foo-might-be-a-cool-thing" } },
            new Case { Raw = "personal/chto/working-on-foo-but-dont-have-a-clear-spec-or-issue-number-yet", Parsed = new StructuredBranch { Namespace = "personal/chto", Name = "working-on-foo-but-dont-have-a-clear-spec-or-issue-number-yet" } },
            new Case { Raw = "feature/fancy-new-foo-E-99998", Parsed = new StructuredBranch { Namespace = "feature", Name = "fancy-new-foo", TicketIdentifier = "E-99998" } },
            new Case { Raw = "feature/fancy-new-foo-E-99998-15.14", Parsed = new StructuredBranch { Namespace = "feature", Name = "fancy-new-foo", TicketIdentifier = "E-99998", TargetRelease = "15.14" } },
            new Case { Raw = "bugfix/foo-was-a-bit-buggy-E-99999", Parsed = new StructuredBranch { Namespace = "bugfix", Name = "foo-was-a-bit-buggy", TicketIdentifier = "E-99999" } },
            new Case { Raw = "bugfix/foo-was-a-bit-buggy-E-99999-2", Parsed = new StructuredBranch { Namespace = "bugfix", Name = "foo-was-a-bit-buggy", TicketIdentifier = "E-99999", NumericSuffix = "2" } },
            new Case { Raw = "bugfix/foo-was-a-bit-buggy-E-99999-2-14.15", Parsed = new StructuredBranch { Namespace = "bugfix", Name = "foo-was-a-bit-buggy", TicketIdentifier = "E-99999", NumericSuffix = "2", TargetRelease = "14.15" } },
            new Case { Raw = "refactor/foo-lets-us-do-some-other-stuff-in-a-much-better-way", Parsed = new StructuredBranch { Namespace = "refactor", Name = "foo-lets-us-do-some-other-stuff-in-a-much-better-way" } },
            new Case { Raw = "refactor/foo-lets-us-do-some-other-stuff-in-a-much-better-way-20160112-1137", Parsed = new StructuredBranch {  Namespace = "refactor", Name = "foo-lets-us-do-some-other-stuff-in-a-much-better-way", NumericSuffix = "20160112-1137" } }
        };

        [Test]
        public void CanParse([ValueSource(nameof(Cases))] Case testCase)
        {
            var parsed = StructuredBranch.Parse(testCase.Raw);
            Assert.That(parsed.Namespace, Is.EqualTo(testCase.Parsed.Namespace));
            Assert.That(parsed.Name, Is.EqualTo(testCase.Parsed.Name));
            Assert.That(parsed.TicketIdentifier, Is.EqualTo(testCase.Parsed.TicketIdentifier));
            Assert.That(parsed.NumericSuffix, Is.EqualTo(testCase.Parsed.NumericSuffix));
            Assert.That(parsed.TargetRelease, Is.EqualTo(testCase.Parsed.TargetRelease));
        }

        [Test]
        public void CanFormat([ValueSource(nameof(Cases))] Case testCase)
        {
            Assert.That(testCase.Parsed.ToString(), Is.EqualTo(testCase.Raw));
        }
    }
}
