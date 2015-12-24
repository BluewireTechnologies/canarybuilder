using System;
using System.Collections.Generic;

namespace Bluewire.Common.Console.Client.Shell
{
    public interface IOutputPipe : IObservable<string>
    {
        IObservable<string> StopBuffering();
        IEnumerable<string> ToUnbufferedEnumerable();
    }
}