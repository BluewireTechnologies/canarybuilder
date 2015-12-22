using System;
using System.IO;

namespace CanaryBuilder.Common.Git
{
    public class GitException : ApplicationException
    {
        public CommandLine CommandLine { get; private set; }
        public int ExitCode { get; private set; }
        public string Messages { get; private set; }

        public GitException(CommandLine commandLine, int exitCode, string messages) : base(messages)
        {
            CommandLine = commandLine;
            ExitCode = exitCode;
            Messages = messages;
        }

        public virtual void Explain(TextWriter writer)
        {
            writer.WriteLine($"Git exited with code {ExitCode}.");
            writer.WriteLine(Messages);
            writer.WriteLine($"Arguments: {CommandLine.GetQuotedArguments()}");
        }
    }
}