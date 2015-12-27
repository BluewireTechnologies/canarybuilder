using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Bluewire.Common.Console.Client.Util
{
    /// <summary>
    /// Implements a ReplaySubject-like system with a controllable buffer.
    /// </summary>
    /// <remarks>
    /// The buffer can be cleared, preventing notifications already received from being replayed.
    /// Alternatively it can be detached entirely, causing the observable to become 'hot'.
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class ControllableReplaySubject<T> : IObservable<T>, IObserver<T>
    {
        private readonly Subject<T> multicast = new Subject<T>();
        private List<T> buffer = new List<T>();
        
        public void Unbuffer()
        {
            lock (this)
            {
                buffer = null;
            }
        }

        public void ClearBuffer()
        {
            lock (this)
            {
                if (buffer == null) return;
                buffer = new List<T>();
            }
        }
        
        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock(this)
            {
                if (buffer == null) return multicast.Subscribe(observer);

                return GetSingleUseBufferedSequence().Subscribe(observer);
            }
        }

        public void OnNext(T value)
        {
            lock(this)
            {
                buffer?.Add(value);
            }
            multicast.OnNext(value);
        }

        public void OnError(Exception error)
        {
            lock (this)
            {
                multicast.OnError(error);
            }
        }

        public void OnCompleted()
        {
            lock(this)
            {
                multicast.OnCompleted();
            }
        }

        private IObservable<T> GetSingleUseBufferedSequence()
        {
            var currentBuffer = buffer;
            var size = buffer.Count;
            var bufferedMulticast = multicast.BufferUntilSubscribed();
            var bufferSubscription = bufferedMulticast.Connect();

            return Observable.Create<T>(obs =>
            {
                var i = 0;
                while (i < size)
                {
                    obs.OnNext(currentBuffer[i]);
                    i++;
                }

                return new CompositeDisposable(
                    bufferedMulticast.Subscribe(obs),
                    bufferSubscription);
            });
        }
    }
}