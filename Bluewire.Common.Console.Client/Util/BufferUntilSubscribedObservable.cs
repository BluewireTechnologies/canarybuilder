using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Bluewire.Common.Console.Client.Util
{
    /// <summary>
    /// http://stackoverflow.com/questions/24790191/hot-concat-in-rx
    /// Contains some modifications to handle deferring OnComplete and OnError as well.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    class BufferUntilSubscribedObservable<T> : IConnectableObservable<T>
    {
        private readonly IObservable<T> _source;
        private readonly IScheduler _scheduler;
        private readonly Subject<Notification<T>> _liveEvents;
        private bool _observationsStarted;
        private Queue<Notification<T>> _buffer;
        private readonly object _gate;

        public BufferUntilSubscribedObservable(IObservable<T> source, IScheduler scheduler)
        {
            _source = source;
            _scheduler = scheduler;
            _liveEvents = new Subject<Notification<T>>();
            _buffer = new Queue<Notification<T>>();
            _gate = new object();
            _observationsStarted = false;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (_gate)
            {
                if (_observationsStarted)
                {
                    return _liveEvents.Dematerialize().Subscribe(observer);
                }

                _observationsStarted = true;

                var bufferedEvents = GetBuffers().Concat().Finally(RemoveBuffer); // Finally clause to remove the buffer if the first observer stops listening.
                return _liveEvents.Merge(bufferedEvents).Dematerialize().Subscribe(observer);
            }
        }

        public IDisposable Connect()
        {
            return _source.Materialize().Subscribe(OnNext);
        }

        private void RemoveBuffer()
        {
            lock (_gate)
            {
                _buffer = null;
            }
        }

        /// <summary>
        /// Acquires a lock and checks the buffer.  If it is empty, then replaces it with null and returns null.  Else replaces it with an empty buffer and returns the old buffer.
        /// </summary>
        /// <returns></returns>
        private Queue<Notification<T>> GetAndReplaceBuffer()
        {
            lock (_gate)
            {
                if (_buffer == null)
                {
                    return null;
                }

                if (_buffer.Count == 0)
                {
                    _buffer = null;
                    return null;
                }

                var result = _buffer;
                _buffer = new Queue<Notification<T>>();
                return result;
            }
        }

        /// <summary>
        /// An enumerable of buffers that will complete when a call to GetAndReplaceBuffer() returns a null, e.g. when the observer has caught up with the incoming source data.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<IObservable<Notification<T>>> GetBuffers()
        {
            Queue<Notification<T>> buffer;
            while ((buffer = GetAndReplaceBuffer()) != null)
            {
                yield return buffer.ToObservable(_scheduler);
            }
        }

        private void OnNext(Notification<T> item)
        {
            lock (_gate)
            {
                if (_buffer != null)
                {
                    _buffer.Enqueue(item);
                    return;
                }
            }

            _liveEvents.OnNext(item);
        }
    }
}