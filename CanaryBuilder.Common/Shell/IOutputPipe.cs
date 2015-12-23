using System;
using System.Collections.Generic;

namespace CanaryBuilder.Common.Shell
{
    public interface IOutputPipe : IObservable<string>
    {
        IObservable<string> StopBuffering();
        IEnumerable<string> ToUnbufferedEnumerable();
    }
}