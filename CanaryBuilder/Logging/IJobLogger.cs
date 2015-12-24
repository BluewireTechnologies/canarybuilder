using System;
using System.IO;
using Bluewire.Common.Console.Client.Shell;

namespace CanaryBuilder.Logging
{
    public interface IJobLogger : IConsoleInvocationLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);

        IDisposable EnterScope(string message);
    }
}