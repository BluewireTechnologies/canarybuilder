using System;
using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using NUnit.Framework;

namespace Bluewire.Common.Console.Client.UnitTests
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
            var exitCode = await process.Completed;

            Assert.That(exitCode, Is.EqualTo(1));
        }

        [Test]
        public async Task CollectsStdOut()
        {
            var invoker = new CommandLineInvoker();
            
            var process = invoker.Start(new CommandLine(CMD_EXE, "/C", "cd"));
            await process.Completed;
            

            Assert.That(await process.StdOut.ToStringAsync(), Is.EqualTo($"{invoker.WorkingDirectory}{Environment.NewLine}"));
        }

        [Test]
        public async Task CollectsStdErr()
        {
            var invoker = new CommandLineInvoker();
            Assume.That(!Directory.Exists(Path.Combine(invoker.WorkingDirectory, "doesnotexist")));
            
            var process = invoker.Start(new CommandLine(CMD_EXE, "/C", "cd", "doesnotexist"));
            await process.Completed;

            Assert.That(await process.StdErr.ToStringAsync(), Is.EqualTo($"The system cannot find the path specified.{Environment.NewLine}"));
        }

        [Test]
        public async Task KillingProcessSetsCompleted()
        {
            var invoker = new CommandLineInvoker();
            
            var process = invoker.Start(new CommandLine(CMD_EXE, "/C", "pause"));
            process.Kill();
            var exitCode = await process.Completed;

            Assert.That(exitCode, Is.EqualTo(-1));
        }

        [Test]
        public async Task AcceptsRawCommandLine()
        {
            var invoker = new CommandLineInvoker();
            Assume.That(!Directory.Exists(Path.Combine(invoker.WorkingDirectory, "doesnotexist")));

            var process = invoker.Start(CommandLine.CreateRaw(CMD_EXE, "/C cd"));
            var exitCode = await process.Completed;

            Assert.That(exitCode, Is.EqualTo(0));
        }
    }
}
