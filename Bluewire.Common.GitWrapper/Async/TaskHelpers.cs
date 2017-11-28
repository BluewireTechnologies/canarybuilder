using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper.Async
{
    public static class TaskHelpers
    {
        public static Task AsTask(this WaitHandle waitHandle, CancellationToken token = default(CancellationToken))
        {
            // Infinite timeout:
            return AsTask(waitHandle, -1, token);
        }

        public static Task AsTask(this WaitHandle waitHandle, TimeSpan limit, CancellationToken token = default(CancellationToken))
        {
            return AsTask(waitHandle, (long)limit.TotalMilliseconds, token);
        }

        private static async Task AsTask(WaitHandle waitHandle, long limitMilliseconds, CancellationToken token = default(CancellationToken))
        {
            // Complete immediately if possible.
            // Simplifies mocking a single-threaded TaskScheduler, since it removes the need to tolerate continuations
            // being queued from other threads eg. the threadpool.
            token.ThrowIfCancellationRequested();
            if (waitHandle.WaitOne(TimeSpan.Zero))
            {
                // In case of races when waiting on the CancellationToken's WaitHandle:
                token.ThrowIfCancellationRequested();
                return;
            }

            var tcs = new TaskCompletionSource<object>();
            using (token.Register(() => tcs.TrySetCanceled()))
            {
                ThreadPool.RegisterWaitForSingleObject(
                    waitHandle,
                    (o, timeout) =>
                    {
                        if (timeout) tcs.TrySetException(new TimeoutException());
                        else tcs.TrySetResult(null);
                    },
                    null,
                    limitMilliseconds,
                    true);

                await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}
