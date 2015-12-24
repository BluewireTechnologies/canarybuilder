namespace CanaryBuilder.Common.Shell
{
    public interface ICommandLine
    {
        string ProgramPath { get; }
        string GetQuotedArguments();
    }
}