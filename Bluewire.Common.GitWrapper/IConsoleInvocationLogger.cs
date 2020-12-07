using CliWrap;

namespace Bluewire.Common.GitWrapper
{
    public interface IConsoleInvocationLogger
    {
        IConsoleInvocationLogScope Create();
        IConsoleInvocationLogScope CreateMinor();
    }

    public interface IConsoleInvocationLogScope
    {
        Command LogOutputs(Command command);
        void LogResult(CommandResult result, bool ignoreExitCode);
    }
}
