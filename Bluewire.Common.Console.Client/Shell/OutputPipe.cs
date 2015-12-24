using System;
using System.Collections.Generic;
using Bluewire.Common.Console.Client.Util;

namespace Bluewire.Common.Console.Client.Shell
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
    /// asynchronously with appropriate subscriptions followed by StopBuffering().
    /// For small outputs it may be appropriate to consume the buffer via ToEnumerable()
    /// after the stream has ended.
    /// </remarks>
    public class OutputPipe : IOutputPipe, IDisposable
    {
        private readonly SwitchedBufferedObservable<string> pipe;
        private IDisposable subscription;

        public OutputPipe(IObservable<string> lineSource)
        {
            pipe = new SwitchedBufferedObservable<string>();
            subscription = lineSource.Subscribe(pipe);
        }

        public void StopBuffering()
        {
            pipe.StopBuffering();
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