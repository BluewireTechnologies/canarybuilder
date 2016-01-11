using System;
using System.Reactive.Disposables;

namespace Bluewire.Common.Console.Client.Shell
{
    public class NoConsoleInvocationLogger : IConsoleInvocationLogger
    {
        public IDisposable LogInvocation(IConsoleProcess process)
        {
            return Disposable.Empty;
        }

        public IDisposable LogMinorInvocation(IConsoleProcess process)
        {
            return Disposable.Empty;
        }
    }
}