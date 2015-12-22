using System;

namespace CanaryBuilder.Parsers
{
    public class JobScriptSyntaxErrorException : JobScriptException
    {
        public ScriptLine Line { get; }

        public JobScriptSyntaxErrorException(ScriptLine line, Exception innerException) : this(line, innerException.Message)
        {
        }

        public JobScriptSyntaxErrorException(ScriptLine line, string message) : base(line.LineNumber, message)
        {
            Line = line;
        }
    }
}