using System.IO;
using System.Linq;
using Bluewire.Common.GitWrapper.Model;
using CanaryBuilder.Merge;
using CanaryBuilder.Parsers;
using NUnit.Framework;

namespace CanaryBuilder.UnitTests.Merge
{


    [TestFixture]
    public class MergeJobParserTests
    {
        private static MergeJobParser parser = new MergeJobParser();

        [Test]
        public void UnrecognisedDirectiveIsNotPermitted()
        {
            Assert.Throws<JobScriptSyntaxErrorException>(() =>
                parser.Parse(new StringReader(@"
                    out of band: some comment here
                ")));
        }

        [TestFixture]
        public class StartAt
        {
            [Test]
            public void SingleUseIsValid()
            {
                var definition = parser.Parse(new StringReader(@"
                    start at: master
                "));
                parser.Validate(definition);

                Assert.That(definition.Base, Is.EqualTo(new Ref("master")));
            }

            [Test]
            public void MultipleUsesAreNotPermitted()
            {
                Assert.Throws<DuplicateDirectiveException>(() =>
                    parser.Parse(new StringReader(@"
                        start at: master
                        start at: bugfix/something
                    ")));
            }

            [Test]
            public void TrailingCharactersAreNotPermitted()
            {
                Assert.Throws<JobScriptSyntaxErrorException>(() =>
                    parser.Parse(new StringReader(@"
                        start at: master with some other stuff
                    ")));
            }
        }

        [TestFixture]
        public class ProduceBranch
        {
            [Test]
            public void DirectiveIsRecognised()
            {
                var definition = parser.Parse(new StringReader(@"
                    produce branch temporary
                "));

                Assert.That(definition.FinalBranch, Is.EqualTo(new Ref("temporary")));
            }

            [Test]
            public void MultipleUsesAreNotPermitted()
            {
                Assert.Throws<DuplicateDirectiveException>(() =>
                    parser.Parse(new StringReader(@"
                        produce branch: canary/something
                        produce branch temporary
                    ")));
            }

            [Test]
            public void TrailingCharactersAreNotPermitted()
            {
                Assert.Throws<JobScriptSyntaxErrorException>(() =>
                    parser.Parse(new StringReader(@"
                        produce branch: test-branch with some other stuff
                    ")));
            }
        }

        [TestFixture]
        public class ProduceTag
        {
            [Test]
            public void DirectiveIsRecognised()
            {
                var definition = parser.Parse(new StringReader(@"
                    produce tag temporary
                "));

                Assert.That(definition.FinalTag, Is.EqualTo(new Ref("temporary")));
            }

            [Test]
            public void MultipleUsesAreNotPermitted()
            {
                Assert.Throws<DuplicateDirectiveException>(() =>
                    parser.Parse(new StringReader(@"
                        produce tag: canary/something
                        produce tag temporary
                    ")));
            }

            [Test]
            public void TrailingCharactersAreNotPermitted()
            {
                Assert.Throws<JobScriptSyntaxErrorException>(() =>
                    parser.Parse(new StringReader(@"
                        produce tag: test-tag with some other stuff
                    ")));
            }
        }

        [TestFixture]
        public class Merge
        {
            [Test]
            public void DirectiveIsRecognised()
            {
                var definition = parser.Parse(new StringReader(@"
                    merge bugfix/source-branch
                "));

                Assert.That(definition.Merges.Select(m => m.Ref), Is.EqualTo(new[] { new Ref("bugfix/source-branch") }));
            }

            [Test]
            public void MultipleUsesAreRecognised()
            {
                var definition = parser.Parse(new StringReader(@"
                    merge bugfix/how-did-this-get-here
                    merge: feature/for-friday
                    merge: refactor/fix-everything
                    merge bugfix/run-screaming
                "));

                Assert.That(definition.Merges.Select(m => m.Ref), Is.EqualTo(new[] {
                    new Ref("bugfix/how-did-this-get-here"),
                    new Ref("feature/for-friday"),
                    new Ref("refactor/fix-everything"),
                    new Ref("bugfix/run-screaming")
                }));
            }

            [Test]
            public void TrailingCharactersAreNotPermitted()
            {
                Assert.Throws<JobScriptSyntaxErrorException>(() =>
                    parser.Parse(new StringReader(@"
                        produce tag: test-tag with some other stuff
                    ")));
            }
        }

        [TestFixture]
        public class VerifyWith
        {
            [Test]
            public void AppliesToInitialState()
            {
                var definition = parser.Parse(new StringReader(@"
                    start at: master
                    verify with: TestBuild
                "));
                parser.Validate(definition);

                var verifier = AssertVerifierOfType<CommandLineWorkingCopyVerifier>(definition.Verifier);
                Assert.That(verifier.Command.TargetFilePath, Is.EqualTo("TestBuild"));
            }

            [Test]
            public void DoesNotApplyToMerges()
            {
                var definition = parser.Parse(new StringReader(@"
                    start at: master
                    verify with: TestBuild
                    merge: something/something
                "));
                parser.Validate(definition);

                Assert.That(definition.Merges.Single().Verifier, Is.Null);
            }

            [Test]
            public void QuotedProgramPathCanContainWhitespace()
            {
                var definition = parser.Parse(new StringReader(@"
                    start at: master
                    verify with ""C:\Program Files\TestBuild""
                    merge: something/something
                "));
                parser.Validate(definition);

                var verifier = AssertVerifierOfType<CommandLineWorkingCopyVerifier>(definition.Verifier);
                Assert.That(verifier.Command.TargetFilePath, Is.EqualTo(@"C:\Program Files\TestBuild"));
            }

            [Test]
            public void QuotedArgumentsCanContainWhitespace()
            {
                var definition = parser.Parse(new StringReader(@"
                    start at: master
                    verify with ""C:\Program Files\TestBuild"" ""file name""
                    merge: something/something
                "));
                parser.Validate(definition);

                var verifier = AssertVerifierOfType<CommandLineWorkingCopyVerifier>(definition.Verifier);
                Assert.That(verifier.Command.Arguments, Is.EqualTo(@"""file name"""));
            }
        }


        [TestFixture]
        public class VerifyMergesWith
        {
            [Test]
            public void AppliesToSubsequentMergeDirectives()
            {
                var definition = parser.Parse(new StringReader(@"
                    start at: master
                    merge: something/first
                    verify merges with: TestBuild
                    merge: something/second
                "));
                parser.Validate(definition);

                Assert.That(definition.Merges.ElementAt(0).Verifier, Is.Null);

                var verifier = AssertVerifierOfType<CommandLineWorkingCopyVerifier>(definition.Merges.ElementAt(1).Verifier);
                Assert.That(verifier.Command.TargetFilePath, Is.EqualTo("TestBuild"));
            }

            [Test]
            public void QuotedProgramPathCanContainWhitespace()
            {
                var definition = parser.Parse(new StringReader(@"
                    start at: master
                    verify merges with ""C:\Program Files\TestBuild""
                    merge: something/something
                "));
                parser.Validate(definition);

                var verifier = AssertVerifierOfType<CommandLineWorkingCopyVerifier>(definition.Merges.Single().Verifier);
                Assert.That(verifier.Command.TargetFilePath, Is.EqualTo(@"C:\Program Files\TestBuild"));
            }

            [Test]
            public void QuotedArgumentsCanContainWhitespace()
            {
                var definition = parser.Parse(new StringReader(@"
                    start at: master
                    verify merges with ""C:\Program Files\TestBuild"" ""file name""
                    merge: something/something
                "));
                parser.Validate(definition);

                var verifier = AssertVerifierOfType<CommandLineWorkingCopyVerifier>(definition.Merges.Single().Verifier);
                Assert.That(verifier.Command.Arguments, Is.EqualTo(@"""file name"""));
            }
        }

        private static T AssertVerifierOfType<T>(IWorkingCopyVerifier verifier) where T : IWorkingCopyVerifier
        {
            Assert.That(verifier, Is.InstanceOf<T>());
            return (T)verifier;
        }
    }
}
