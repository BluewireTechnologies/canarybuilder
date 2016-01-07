using System;
using System.Collections;
using System.Collections.Generic;
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
    public class CommandLine : ICommandLine, IEnumerable
    {
        public CommandLine(string programPath, params string[] arguments)
        {
            ProgramPath = programPath;
            AddList(arguments);
        }

        public CommandLine Add(string arg)
        {
            arguments.Add(arg);
            return this;
        }

        public CommandLine AddList(IEnumerable<string> list)
        {
            this.arguments.AddRange(list);
            return this;
        }

        private readonly List<string> arguments = new List<string>();

        public string ProgramPath { get; }
        public string[] Arguments => arguments.ToArray();

        public string GetQuotedArguments()
        {
            return String.Join(" ", arguments.Select(Quote));
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return arguments.GetEnumerator();
        }

        /// <summary>
        /// Creates an immutable copy of this command line.
        /// </summary>
        /// <returns></returns>
        public ICommandLine Seal()
        {
            return new CommandLineWithRawArgumentString(ProgramPath, GetQuotedArguments());
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
            return new CommandLineWithRawArgumentString(programPath, rawArguments);
        }

        /// <summary>
        /// Immutable command line object with a pre-quoted argument string.
        /// </summary>
        class CommandLineWithRawArgumentString : ICommandLine
        {
            private readonly string rawArguments;

            public CommandLineWithRawArgumentString(string programPath, string rawArguments)
            {
                this.rawArguments = rawArguments;
                ProgramPath = programPath;
            }

            public string ProgramPath { get; }

            public string GetQuotedArguments()
            {
                return rawArguments;
            }

            public ICommandLine Seal()
            {
                return this;
            }

            public override string ToString()
            {
                return $"{Quote(ProgramPath)} {rawArguments}";
            }
        }
    }
}