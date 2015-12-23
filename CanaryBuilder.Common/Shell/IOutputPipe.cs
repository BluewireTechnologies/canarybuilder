using System;
using System.Collections.Generic;

namespace CanaryBuilder.Common.Shell
{
    public interface IOutputPipe : IObservable<string>
    {
        void StopBuffering();
        IEnumerable<string> ToUnbufferedEnumerable();
    }
}