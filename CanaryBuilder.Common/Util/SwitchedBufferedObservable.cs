using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CanaryBuilder.Common.Util
{
    /// <summary>
    /// Implements a Replay() observable which can be 'detached', clearing the buffer
    /// and causing future subscriptions to only receive new events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SwitchedBufferedObservable<T> : IObservable<T>, IObserver<T>
    {
        private readonly Subject<T> multicast = new Subject<T>();
        private List<T> buffer = new List<T>();
        private volatile bool unblocked;
        
        public void StopBuffering()
        {
            lock(buffer)
            {
                if (unblocked) return;
                unblocked = true;
                buffer = new List<T>();
            }
        }
        
        public IEnumerable<T> DetachBufferAndEnumerate()
        {
            lock (buffer)
            {
                if (unblocked) throw new InvalidOperationException("Buffering has already been halted.");
                unblocked = true;

                var currentBuffer = buffer;
                buffer = new List<T>();
                return currentBuffer.Concat(multicast.ToEnumerable());
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock(buffer)
            {
                if (!unblocked)
                {
                    foreach (var item in buffer)
                    {
                        observer.OnNext(item);
                    }
                }
                return multicast.Subscribe(observer);
            }
        }

        public void OnNext(T value)
        {
            if (!unblocked)
            {
                lock(buffer)
                {
                    if (!unblocked)
                    {
                        buffer.Add(value);
                    }
                }
            }
            multicast.OnNext(value);
        }

        public void OnError(Exception error)
        {
            multicast.OnError(error);
        }

        public void OnCompleted()
        {
            multicast.OnCompleted();
        }
    }
}