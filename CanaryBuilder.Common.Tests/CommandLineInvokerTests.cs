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

            var process = invoker.Start(new CommandLine(CMD_EXE, "/C", "cd", "doesnotexist"));
            var exitCode = await process.CompletedAsync();

            Assert.That(exitCode, Is.EqualTo(1));
        }

        [Test]
        public async Task CollectsStdOut()
        {
            var invoker = new CommandLineInvoker();

            var stdout = new StringWriter();
            var process = invoker.Start(new CommandLine(CMD_EXE, "/C", "cd"), stdout);
            await process.CompletedAsync();
            

            Assert.That(stdout.ToString(), Is.EqualTo($"{invoker.WorkingDirectory}{Environment.NewLine}"));
        }

        [Test]
        public async Task CollectsStdErr()
        {
            var invoker = new CommandLineInvoker();
            Assume.That(!Directory.Exists(Path.Combine(invoker.WorkingDirectory, "doesnotexist")));

            var stderr = new StringWriter();
            var process = invoker.Start(new CommandLine(CMD_EXE, "/C", "cd", "doesnotexist"), TextWriter.Null, stderr);
            await process.CompletedAsync();

            Assert.That(stderr.ToString(), Is.EqualTo($"The system cannot find the path specified.{Environment.NewLine}"));
        }
    }
}
