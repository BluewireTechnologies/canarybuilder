using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

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
            return String.Join(" ", Arguments.Select(Quote));
        }

        private static readonly Regex rxSimpleArgument = new Regex(@"^[-\w\d]+$", RegexOptions.Compiled);

        private static string Quote(string arg)
        {
            if (rxSimpleArgument.IsMatch(arg)) return arg;   
            return $"\"{arg.Replace("\"", "\"\"")}\"";
        }
    }
}