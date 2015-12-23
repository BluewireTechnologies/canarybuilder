using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CanaryBuilder.Common.Util;

namespace CanaryBuilder.Common
{
    public class CommandLineInvoker
    {
        public string WorkingDirectory { get; }

        public CommandLineInvoker() : this(Directory.GetCurrentDirectory())
        {
        }

        public CommandLineInvoker(string workingDirectory)
        {
            if (workingDirectory == null) throw new ArgumentNullException(nameof(workingDirectory));
            this.WorkingDirectory = workingDirectory;
        }

        public IConsoleProcess Start(CommandLine cmd, TextWriter stdout = null, TextWriter stderr = null)
        {
            var info = new ProcessStartInfo(cmd.ProgramPath, cmd.GetQuotedArguments())
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = WorkingDirectory
            };
            var process = Process.Start(info);
            CollectOutput(process, stdout, stderr);
            return new ConsoleProcess(cmd, process);
        }

        private static void CollectOutput(Process process, TextWriter stdout = null, TextWriter stderr = null)
        {
            stdout = stdout ?? TextWriter.Null;
            stderr = stderr ?? TextWriter.Null;
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            // Buffered capture for now. Eventually need to change this to use blocking streams.
            process.OutputDataReceived += (s, e) => {
                if (e.Data != null) stdout.WriteLine(e.Data);
            };
            process.ErrorDataReceived += (s, e) => {
                if (e.Data != null) stderr.WriteLine(e.Data);
            };
        }
        
        class ConsoleProcess : IConsoleProcess
        {
            private readonly Process process;

            public ConsoleProcess(CommandLine commandLine, Process process)
            {
                CommandLine = commandLine;
                this.process = process;
            }

            public CommandLine CommandLine { get; }
            public async Task<int> CompletedAsync()
            {
                return await process.WaitForExitAsync(CancellationToken.None);
            }

            public async Task<int> CompletedAsync(CancellationToken token)
            {
                return await process.WaitForExitAsync(token);
            }
        }
    }
}