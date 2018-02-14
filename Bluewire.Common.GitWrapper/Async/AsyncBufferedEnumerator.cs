using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper.Async
{
    /// <summary>
    /// Asynchronous implementation of the enumerator pattern which buffers the output of
    /// an observable stream. Whether the stream is hot or cold, items should never be dropped.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AsyncBufferedEnumerator<T> : IAsyncEnumerator<T>
    {
        private bool disposed;
        private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
        private readonly AutoResetEvent queued = new AutoResetEvent(false);
        private readonly BlockingCollection<T> queue;

        /// <summary>
        /// Create a buffered, asynchronous enumerator from the specified observable.
        /// </summary>
        /// <param name="observable"></param>
        /// <param name="bufferLength"></param>
        public AsyncBufferedEnumerator(IObservable<T> observable, int bufferLength = 256)
        {
            queue = new BlockingCollection<T>(bufferLength);
            observable.Synchronize().Subscribe(
                i => {
                    queue.Add(i, shutdown.Token);
                    queued.Set();
                },
                () => {
                    queue.CompleteAdding();
                    queued.Set();
                },
                shutdown.Token);
        }

        public async Task<bool> MoveNext(CancellationToken token = default(CancellationToken))
        {
            while (!queue.IsCompleted)
            {
                T item;
                if (queue.TryTake(out item, -1, token))
                {
                    Current = item;
                    return true;
                }
                await queued.AsTask(token);
            }
            return false;
        }

        public T Current { get; private set; }

        public void Dispose()
        {
            if (disposed) return;
            lock (shutdown)
            {
                if (disposed) return;
                shutdown.Cancel();
                shutdown.Dispose();
                disposed = true;
            }
        }
    }
}
