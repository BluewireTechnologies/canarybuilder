using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace CanaryBuilder.Common.Tests
{
    [TestFixture]
    public class CommandLineInvokerTests
    {
        public CommandLineInvokerTests()
        {
            Assume.That(File.Exists(CMD_EXE));
        }

        private static readonly string CMD_EXE = Path.Combine(Environment.SystemDirectory, "cmd.exe");

        [Test]
        public async Task AwaitsExitAndYieldsExitCode()
        {
            var invoker = new CommandLineInvoker();
            Assume.That(!Directory.Exists(Path.Combine(invoker.WorkingDirectory, "doesnotexist")));

            var exitCode = await invoker.Run(new CommandLine(CMD_EXE, "/C", "cd", "doesnotexist"), CancellationToken.None);

            Assert.That(exitCode, Is.EqualTo(1));
        }

        [Test]
        public async Task CollectsStdOut()
        {
            var invoker = new CommandLineInvoker();

            var stdout = new StringWriter();
            await invoker.Run(new CommandLine(CMD_EXE, "/C", "cd"), CancellationToken.None, stdout);
            

            Assert.That(stdout.ToString(), Is.EqualTo($"{invoker.WorkingDirectory}{Environment.NewLine}"));
        }

        [Test]
        public async Task CollectsStdErr()
        {
            var invoker = new CommandLineInvoker();
            Assume.That(!Directory.Exists(Path.Combine(invoker.WorkingDirectory, "doesnotexist")));

            var stderr = new StringWriter();
            await invoker.Run(new CommandLine(CMD_EXE, "/C", "cd", "doesnotexist"), CancellationToken.None, TextWriter.Null, stderr);


            Assert.That(stderr.ToString(), Is.EqualTo($"The system cannot find the path specified.{Environment.NewLine}"));
        }
    }
}
