namespace Bluewire.Common.Console.Client.Shell
{
    public static class CommandLineExtensions
    {
        public static IConsoleProcess Run(this ICommandLine cmd)
        {
            return new CommandLineInvoker().Start(cmd);
        }

        public static IConsoleProcess RunFrom(this ICommandLine cmd, string workingDirectory)
        {
            return new CommandLineInvoker(workingDirectory).Start(cmd);
        }
    }
}