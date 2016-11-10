using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper.Async
{
    public static class TaskHelpers
    {
        public static Task AsTask(this WaitHandle waitHandle, CancellationToken token = default(CancellationToken))
        {
            return AsTask(waitHandle, TimeSpan.FromMilliseconds(Int32.MaxValue), token);
        }

        public static async Task AsTask(this WaitHandle waitHandle, TimeSpan limit, CancellationToken token = default(CancellationToken))
        {
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
                    limit,
                    true);

                await tcs.Task;
            }
        }
    }
}
