using System;

namespace CanaryBuilder.Parsers
{
    public abstract class JobScriptException : ApplicationException
    {
        protected JobScriptException(string message) : base(message)
        {
        }

        protected JobScriptException(int lineNumber, string message) : base($"Line {lineNumber}: {message}")
        {
        }
    }
}

