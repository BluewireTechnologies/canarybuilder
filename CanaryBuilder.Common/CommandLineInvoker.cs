using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CanaryBuilder.Common
{
    public class CommandLineInvoker
    {
        public async Task<int> Run(CommandLine cmd, CancellationToken cancelToken, TextWriter stdout = null, TextWriter stderr = null)
        {
            var info = new ProcessStartInfo(cmd.ProgramPath, cmd.GetQuotedArguments())
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var process = Process.Start(info);
            CollectOutput(process, stdout, stderr);
            return await WaitForExitAsync(process, cancelToken);
        }

        private static void CollectOutput(Process process, TextWriter stdout = null, TextWriter stderr = null)
        {
            stdout = stdout ?? TextWriter.Null;
            stderr = stderr ?? TextWriter.Null;
            
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            // Buffered capture for now. Eventually need to change this to use blocking streams.
            process.OutputDataReceived += (s, e) => {
                if (e.Data != null) stdout.Write(e.Data);
            };
            process.ErrorDataReceived += (s, e) => {
                if (e.Data != null) stderr.Write(e.Data);
            };
        }

        private static Task<int> WaitForExitAsync(Process process, CancellationToken cancelToken)
        {
            var tcs = new TaskCompletionSource<int>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(process.ExitCode);
            // Handle possible race condition if process has already terminated.
            if (process.HasExited)
            {
                tcs.TrySetResult(process.ExitCode);
            }
            cancelToken.Register(() => tcs.TrySetCanceled());
            return tcs.Task;
        }
    }
}