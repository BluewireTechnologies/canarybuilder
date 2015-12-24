using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace CanaryBuilder.Common.Shell
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

        public IConsoleProcess Start(ICommandLine cmd)
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
            return new ConsoleProcess(cmd, process);
        }
        
        class ConsoleProcess : IConsoleProcess
        {
            private readonly Process process;
            private readonly TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            private bool completeAndFlushed = false;
            private readonly OutputPipe stdOutPipe;
            private readonly OutputPipe stdErrPipe;

            public ConsoleProcess(ICommandLine commandLine, Process process)
            {
                CommandLine = commandLine;
                this.process = process;
                
                stdOutPipe = ObservePipe(h => process.OutputDataReceived += h, h => process.OutputDataReceived -= h);
                stdErrPipe = ObservePipe(h => process.ErrorDataReceived += h, h => process.ErrorDataReceived -= h);

                process.Exited += (s, e) => { OnTerminated(); };
                process.EnableRaisingEvents = true;
                
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                // Handle possible race condition if process has already terminated.
                if (process.HasExited)
                {
                    OnTerminated();
                }
            }

            private static OutputPipe ObservePipe(Action<DataReceivedEventHandler> attach, Action<DataReceivedEventHandler> detach)
            {
                var raw = Observable.FromEvent<DataReceivedEventHandler, string>(h => (s, e) => h(e.Data), attach, detach);
                return new OutputPipe(raw.Where(d => d != null));
            }

            private void OnTerminated()
            {
                lock(this)
                {
                    if (completeAndFlushed) return;
                    // Flush the output buffers before reporting completion.
                    process.WaitForExit();
                    stdOutPipe.Complete();
                    stdErrPipe.Complete();
                }
                tcs.TrySetResult(process.ExitCode);
                completeAndFlushed = true;
            }
            
            public IOutputPipe StdOut => stdOutPipe;
            public IOutputPipe StdErr => stdErrPipe;

            public void Kill()
            {
                if (completeAndFlushed) return;
                process.Kill();
            }

            public ICommandLine CommandLine { get; }
            public Task<int> Completed => tcs.Task;
        }
    }
}