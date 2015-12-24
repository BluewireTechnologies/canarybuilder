using Bluewire.Common.Console.Client.Shell;
using NUnit.Framework;

namespace Bluewire.Common.Console.Client.UnitTests.Shell
{
    [TestFixture]
    public class RawCommandLineTests
    {
        [Test]
        public void ArgumentStringIsNotQuoted()
        {
            var cmd = CommandLine.CreateRaw(@"d:\git.exe", "raw argument string with --options and orphaned \" quotes");

            Assert.That(cmd.GetQuotedArguments(), Is.EqualTo("raw argument string with --options and orphaned \" quotes"));
        }
        
        [Test]
        public void StringRepresentationOfObjectQuotesProgramPathsWithWhitespace()
        {
            var cmd = CommandLine.CreateRaw(@"d:\program files\git.exe", "status");

            Assert.That(cmd.ToString(), Is.EqualTo(@"""d:\program files\git.exe"" status"));
        }

        [Test]
        public void StringRepresentationOfObjectDoesNotQuoteSimpleProgramPaths()
        {
            var cmd = CommandLine.CreateRaw(@"d:\git.exe", "status");

            Assert.That(cmd.ToString(), Is.EqualTo(@"d:\git.exe status"));
        }

        [Test]
        public void StringRepresentationOfObjectDoesNotQuoteArgumentString()
        {
            var cmd = CommandLine.CreateRaw(@"d:\git.exe", "raw argument string with --options");

            Assert.That(cmd.ToString(), Is.EqualTo(@"d:\git.exe raw argument string with --options"));
        }
    }
}
