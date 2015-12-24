﻿using CanaryBuilder.Common.Shell;
using NUnit.Framework;

namespace CanaryBuilder.Common.UnitTests
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
        public void CreatesOrderedListFromMultipleArguments()
        {
            var cmd = new CommandLine(@"d:\git.exe", "a", "--b", "-c");

            Assert.That(cmd.GetQuotedArguments(), Is.EqualTo("a --b -c"));
        }
    }
}