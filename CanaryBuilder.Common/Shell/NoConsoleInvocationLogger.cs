using System;
using System.Reactive.Disposables;

namespace CanaryBuilder.Common.Shell
{
    public class NoConsoleInvocationLogger : IConsoleInvocationLogger
    {
        public IDisposable LogInvocation(IConsoleProcess process)
        {
            return Disposable.Empty;
        }
    }
}