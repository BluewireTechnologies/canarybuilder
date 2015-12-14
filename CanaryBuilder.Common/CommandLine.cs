using System;
using System.Linq;

namespace CanaryBuilder.Common
{
    public class CommandLine
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
            return String.Join(" ", Arguments.Select(o => $"\"{Quote(o)}\""));
        }

        private string Quote(string arg)
        {
            return arg.Replace("\"", "\"\"");
        }
    }
}