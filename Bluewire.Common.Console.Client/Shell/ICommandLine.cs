namespace Bluewire.Common.Console.Client.Shell
{
    public interface ICommandLine
    {
        string ProgramPath { get; }
        string GetQuotedArguments();
        ICommandLine Seal();
    }
}