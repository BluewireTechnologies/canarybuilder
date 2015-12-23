using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CanaryBuilder.Common.Util
{
    public static class ProcessExtensions
    {
        public static Task<int> WaitForExitAsync(this Process process, CancellationToken cancelToken)
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
