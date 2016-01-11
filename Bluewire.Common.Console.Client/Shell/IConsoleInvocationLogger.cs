namespace Bluewire.Common.Console.Client.Shell
{
    public interface IConsoleInvocationLogger
    {
        IConsoleInvocationLogScope LogInvocation(IConsoleProcess process);
        IConsoleInvocationLogScope LogMinorInvocation(IConsoleProcess process);
    }
}