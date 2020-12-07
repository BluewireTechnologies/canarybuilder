using System;
using System.IO;
using CliWrap;

namespace Bluewire.Common.GitWrapper
{
    public class GitException : ApplicationException
    {
        public string CommandArguments { get; }
        public int ExitCode { get; private set; }
        public string Messages { get; private set; }

        public GitException(Command command, int exitCode, string messages) : base(messages)
        {
            CommandArguments = command.Arguments;
            ExitCode = exitCode;
            Messages = messages;
        }

        public virtual void Explain(TextWriter writer)
        {
            writer.WriteLine($"Git exited with code {ExitCode}.");
            writer.WriteLine(Messages);
            writer.WriteLine($"Arguments: {CommandArguments}");
        }
    }
}
