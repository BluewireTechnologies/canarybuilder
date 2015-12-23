using System;

namespace CanaryBuilder.Common.Shell
{
    public interface IConsoleInvocationLogger
    {
        IDisposable LogInvocation(IConsoleProcess process);
    }
}