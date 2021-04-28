using System;
using Bluewire.Common.GitWrapper;

namespace CanaryBuilder.Logging
{
    public interface IJobLogger : IConsoleInvocationLogger
    {
        void Info(string message);
        void Warn(string message, Exception exception = null);
        void Warn(Exception exception);
        void Error(string message, Exception exception = null);
        void Error(Exception exception);

        IDisposable EnterScope(string message);
    }
}
