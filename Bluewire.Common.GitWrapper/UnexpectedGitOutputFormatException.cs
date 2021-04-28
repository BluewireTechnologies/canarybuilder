using System.IO;
using CliWrap;

namespace Bluewire.Common.GitWrapper
{
    /// <summary>
    /// Thrown when the output of a command does not conform to the expected format.
    /// </summary>
    public class UnexpectedGitOutputFormatException : GitException
    {
        public UnexpectedGitOutputFormatDetails[] Details { get; private set; }

        public UnexpectedGitOutputFormatException(Command command, params UnexpectedGitOutputFormatDetails[] details) : base(command, 0, "The output of the command could not be parsed.")
        {
            Details = details;
        }

        public UnexpectedGitOutputFormatException(Command command, string message) : base(command, 0, message)
        {
        }

        public override void Explain(TextWriter writer)
        {
            writer.WriteLine(Message);
            foreach (var detail in Details)
            {
                detail.Explain(writer);
            }
            writer.WriteLine($"Arguments: {CommandArguments}");
        }
    }
}
