using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Bluewire.Common.Console.Client.Shell
{
    /// <summary>
    /// Encapsulates a command with an argument list, to be interpreted by the command shell.
    /// </summary>
    /// <remarks>
    /// Arguments are automatically quoted to retain whitespace, etc.
    /// </remarks>
    public class CommandLine : ICommandLine
    {
        public CommandLine(string programPath, params string[] arguments)
        {
            ProgramPath = programPath;
            Arguments = arguments;
        }

        public string ProgramPath { get; }
        public string[] Arguments { get; }

        public string GetQuotedArguments()
        {
            return String.Join(" ", Arguments.Select(Quote));
        }

        private static readonly Regex rxSimpleArgument = new Regex(@"^[-\w\d/\\:\.]+$", RegexOptions.Compiled);

        public static string Quote(string arg)
        {
            if (rxSimpleArgument.IsMatch(arg)) return arg;   
            return $"\"{arg.Replace("\"", "\"\"")}\"";
        }

        public override string ToString()
        {
            return $"{Quote(ProgramPath)} {GetQuotedArguments()}";
        }

        /// <summary>
        /// Create a command line from a raw, pre-quoted argument string. No additional processing of arguments will be
        /// done; the string will be handed directly to the command interpreter.
        /// </summary>
        /// <remarks>
        /// This is provided for terseness of eg. integration tests, when a series of hardcoded commands may be run in
        /// sequence. Avoid using this with generated strings, especially user input.
        /// </remarks>
        public static ICommandLine CreateRaw(string programPath, string rawArguments)
        {
            return new CommandLineWithUnquotedArgumentString(programPath, rawArguments);
        }

        class CommandLineWithUnquotedArgumentString : ICommandLine
        {
            private readonly string rawArguments;

            public CommandLineWithUnquotedArgumentString(string programPath, string rawArguments)
            {
                this.rawArguments = rawArguments;
                ProgramPath = programPath;
            }

            public string ProgramPath { get; }

            public string GetQuotedArguments()
            {
                return rawArguments;
            }

            public override string ToString()
            {
                return $"{Quote(ProgramPath)} {rawArguments}";
            }
        }
    }
}