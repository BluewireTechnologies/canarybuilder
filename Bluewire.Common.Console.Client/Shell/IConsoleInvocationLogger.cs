using System;

namespace Bluewire.Common.Console.Client.Shell
{
    public interface IConsoleInvocationLogger
    {
        IDisposable LogInvocation(IConsoleProcess process);
        IDisposable LogMinorInvocation(IConsoleProcess process);
    }
}