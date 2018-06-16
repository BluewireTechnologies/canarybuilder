using System.IO;
using Bluewire.Common.Console.Client.Shell;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Thrown when the output of a command does not conform to the expected format.
    /// </summary>
    public class UnexpectedGitOutputFormatException : GitException
    {
        public UnexpectedGitOutputFormatDetails[] Details { get; private set; }

        public UnexpectedGitOutputFormatException(ICommandLine commandLine, params UnexpectedGitOutputFormatDetails[] details) : base(commandLine, 0, "The output of the command could not be parsed.")
        {
            Details = details;
        }

        public UnexpectedGitOutputFormatException(ICommandLine commandLine, string message) : base(commandLine, 0, message)
        {
        }

        public override void Explain(TextWriter writer)
        {
            writer.WriteLine(Message);
            foreach (var detail in Details)
            {
                detail.Explain(writer);
            }
            writer.WriteLine($"Arguments: {CommandLine.GetQuotedArguments()}");
        }
    }
}
