﻿using Bluewire.Common.Console.Client.Shell;
using NUnit.Framework;

namespace Bluewire.Common.Console.Client.UnitTests.Shell
{
    [TestFixture]
    public class CommandLineTests
    {
        [Test]
        public void ArgumentContainingQuotesIsQuoted()
        {
            var cmd = new CommandLine(@"d:\git.exe", "'\"arg");

            Assert.That(cmd.GetQuotedArguments(), Is.EqualTo("\"'\"\"arg\""));
        }

        [Test]
        public void ArgumentContainingWhitespaceIsQuoted()
        {
            var cmd = new CommandLine(@"d:\git.exe", "a b c");

            Assert.That(cmd.GetQuotedArguments(), Is.EqualTo("\"a b c\""));
        }

        [Test]
        public void SimpleStringArgumentIsNotQuoted()
        {
            var cmd = new CommandLine(@"d:\git.exe", "--word");

            Assert.That(cmd.GetQuotedArguments(), Is.EqualTo("--word"));
        }

        [Test]
        public void NullArgumentIsSilentlyIgnored()
        {
            var cmd = new CommandLine(@"d:\git.exe", "--word", null, "test");

            Assert.That(cmd.GetQuotedArguments(), Is.EqualTo("--word test"));
        }

        [Test]
        public void CreatesOrderedListFromMultipleArguments()
        {
            var cmd = new CommandLine(@"d:\git.exe", "a", "--b", "-c");

            Assert.That(cmd.GetQuotedArguments(), Is.EqualTo("a --b -c"));
        }

        [Test]
        public void StringRepresentationOfObjectQuotesProgramPathsWithWhitespace()
        {
            var cmd = new CommandLine(@"d:\program files\git.exe", "status");

            Assert.That(cmd.ToString(), Is.EqualTo(@"""d:\program files\git.exe"" status"));
        }

        [Test]
        public void StringRepresentationOfObjectDoesNotQuoteSimpleProgramPaths()
        {
            var cmd = new CommandLine(@"d:\git.exe", "status");

            Assert.That(cmd.ToString(), Is.EqualTo(@"d:\git.exe status"));
        }

        [Test]
        public void CanUseListInitialiser()
        {
            var cmd = new CommandLine(@"d:\git.exe") { "a", "b\"", "c" };

            Assert.That(cmd.GetQuotedArguments(), Is.EqualTo("a \"b\"\"\" c"));
        }
    }
}
