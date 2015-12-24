using System.IO;
using Bluewire.Common.Console.Client.Shell;

namespace Bluewire.Common.Git
{
    /// <summary>
    /// Thrown when the output of a command does not conform to the expected format.
    /// </summary>
    public class UnexpectedGitOutputFormatException : GitException
    {
        public UnexpectedGitOutputFormatException(ICommandLine commandLine) : base(commandLine, 0, "The output of the command could not be parsed.")
        {
        }

        public override void Explain(TextWriter writer)
        {
            writer.WriteLine(Message);
            writer.WriteLine($"Arguments: {CommandLine.GetQuotedArguments()}");
        }
    }
}