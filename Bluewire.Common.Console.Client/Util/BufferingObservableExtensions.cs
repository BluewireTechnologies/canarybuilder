using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Bluewire.Common.Console.Client.Util
{
    public static class BufferingObservableExtensions
    {
        /// <summary>
        /// Returns a connectable observable, that once connected, will start buffering data until the observer subscribes, at which time it will send all buffered data to the observer and then start sending new data.
        /// Thus the observer may subscribe late to a hot observable yet still see all of the data.  Later observers will not see the buffered events.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="scheduler">Scheduler to use to dump the buffered data to the observer.</param>
        /// <returns></returns>
        public static IConnectableObservable<T> BufferUntilSubscribed<T>(this IObservable<T> source, IScheduler scheduler)
        {
            return new BufferUntilSubscribedObservable<T>(source, scheduler);
        }

        /// <summary>
        /// Returns a connectable observable, that once connected, will start buffering data until the observer subscribes, at which time it will send all buffered data to the observer and then start sending new data.
        /// Thus the observer may subscribe late to a hot observable yet still see all of the data.  Later observers will not see the buffered events.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IConnectableObservable<T> BufferUntilSubscribed<T>(this IObservable<T> source)
        {
            return new BufferUntilSubscribedObservable<T>(source, Scheduler.Immediate);
        }

        public static IObservable<T> HotConcat<T>(params IObservable<T>[] sources)
        {
            var bufferedSources = sources.Select(s => s.BufferUntilSubscribed());
            var subscriptions = new CompositeDisposable(bufferedSources.Select(s => s.Connect()).ToArray());
            return Observable.Create<T>(observer =>
            {
                var s = new SingleAssignmentDisposable();
                subscriptions.Add(s);

                s.Disposable = bufferedSources.Concat().Subscribe(observer);

                return subscriptions;
            });
        }
    }
}