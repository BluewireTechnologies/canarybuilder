using System;
using System.Reactive.Disposables;

namespace Bluewire.Common.Console.Client.Shell
{
    public class NoConsoleInvocationLogger : IConsoleInvocationLogger
    {
        public IConsoleInvocationLogScope LogInvocation(IConsoleProcess process)
        {
            return ConsoleInvocationLogScope.None;
        }

        public IConsoleInvocationLogScope LogMinorInvocation(IConsoleProcess process)
        {
            return ConsoleInvocationLogScope.None;
        }
    }
}