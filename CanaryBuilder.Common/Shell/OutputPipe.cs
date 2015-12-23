using System;
using System.Collections.Generic;
using CanaryBuilder.Common.Util;

namespace CanaryBuilder.Common.Shell
{
    /// <summary>
    /// Buffered wrapper for an output stream.
    /// </summary>
    /// <remarks>
    /// Initially, behaves like a Replay() IObservable in that all lines seen so far are
    /// replayed against new subscribers. After StopBuffering() is called, new subscribers
    /// will not receive any replay and the buffer is thrown away.
    /// 
    /// When the output stream is potentially large, it may be consumed efficiently and
    /// asynchronously with appropriate subscriptions followed by StopBuffering(), or on
    /// a background thread via ToUnbufferedEnumerable().
    /// For small outputs it may be appropriate to consume the buffer via ToEnumerable()
    /// after the stream has ended.
    /// </remarks>
    public class OutputPipe : IOutputPipe, IDisposable
    {
        private SwitchedBufferedObservable<string> pipe;
        private IDisposable subscription;

        public OutputPipe(IObservable<string> lineSource)
        {
            pipe = new SwitchedBufferedObservable<string>();
            subscription = lineSource.Subscribe(pipe);
        }

        public IObservable<string> StopBuffering()
        {
            return pipe.StopBuffering();
        }

        /// <summary>
        /// Enumerates all lines, disabling the buffer once replayed.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ToUnbufferedEnumerable()
        {
            return pipe.DetachBufferAndEnumerate();
        }

        public IDisposable Subscribe(IObserver<string> observer)
        {
            return pipe.Subscribe(observer);
        }

        public void Complete()
        {
            if (subscription == null) return;
            lock (this)
            {
                if (subscription == null) return;
                subscription.Dispose();
                subscription = null;
                pipe.OnCompleted();
            }
        }

        public void Dispose()
        {
            Complete();
        }
    }
}