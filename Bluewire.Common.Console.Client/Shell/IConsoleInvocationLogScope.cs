using System;

namespace Bluewire.Common.Console.Client.Shell
{
    public interface IConsoleInvocationLogScope : IDisposable
    {
        void IgnoreExitCode();
    }
}