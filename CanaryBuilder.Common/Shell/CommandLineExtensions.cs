namespace CanaryBuilder.Common.Shell
{
    public static class CommandLineExtensions
    {
        public static IConsoleProcess Run(this CommandLine cmd)
        {
            return new CommandLineInvoker().Start(cmd);
        }

        public static IConsoleProcess RunFrom(this CommandLine cmd, string workingDirectory)
        {
            return new CommandLineInvoker(workingDirectory).Start(cmd);
        }
    }
}