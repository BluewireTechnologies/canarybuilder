using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using NUnit.Framework;

namespace Bluewire.Common.Console.Client.UnitTests.Shell
{
    [TestFixture]
    public class CommandLineInvokerTests
    {
        public CommandLineInvokerTests()
        {
            Assume.That(Cmd.Exists);
        }
        
        [Test]
        public async Task AwaitsExitAndYieldsExitCode()
        {
            var invoker = new CommandLineInvoker();
            Assume.That(!Directory.Exists(Path.Combine(invoker.WorkingDirectory, "doesnotexist")));

            var process = invoker.Start(new CommandLine(Cmd.GetExecutableFilePath(), "/C", "cd", "doesnotexist"));
            var exitCode = await process.Completed;

            Assert.That(exitCode, Is.EqualTo(1));
        }

        [Test]
        public async Task CollectsStdOut()
        {
            var invoker = new CommandLineInvoker();
            
            var process = invoker.Start(new CommandLine(Cmd.GetExecutableFilePath(), "/C", "cd"));
            await process.Completed;
            

            Assert.That(await process.StdOut.ToStringAsync(), Is.EqualTo($"{invoker.WorkingDirectory}{Environment.NewLine}"));
        }

        [Test]
        public async Task CollectsStdErr()
        {
            var invoker = new CommandLineInvoker();
            Assume.That(!Directory.Exists(Path.Combine(invoker.WorkingDirectory, "doesnotexist")));
            
            var process = invoker.Start(new CommandLine(Cmd.GetExecutableFilePath(), "/C", "cd", "doesnotexist"));
            await process.Completed;

            Assert.That(await process.StdErr.ToStringAsync(), Is.EqualTo($"The system cannot find the path specified.{Environment.NewLine}"));
        }

        [Test]
        public async Task KillingProcessSetsCompleted()
        {
            var invoker = new CommandLineInvoker();
            var process = invoker.Start(new CommandLine(Cmd.GetExecutableFilePath(), "/C", "pause"));

            try
            {
                process.Kill();
            }
            catch (Win32Exception ex) when (ex.Message == "Access is denied")
            {
                Assert.Inconclusive($"Kill() threw an exception: {ex}");
            }

            var exitCode = await process.Completed;

            Assert.That(exitCode, Is.EqualTo(-1));
        }

        [Test]
        public async Task AcceptsRawCommandLine()
        {
            var invoker = new CommandLineInvoker();
            Assume.That(!Directory.Exists(Path.Combine(invoker.WorkingDirectory, "doesnotexist")));

            var process = invoker.Start(CommandLine.CreateRaw(Cmd.GetExecutableFilePath(), "/C cd"));
            var exitCode = await process.Completed;

            Assert.That(exitCode, Is.EqualTo(0));
        }
    }
}
