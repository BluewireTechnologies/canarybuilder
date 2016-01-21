namespace CanaryBuilder.Parsers
{
    public class MissingParameterException : JobScriptException
    {
        public MissingParameterException(string message) : base(message)
        {
        }

        public MissingParameterException(ScriptLine content, string message) : base(content.LineNumber, message)
        {
        }
    }
}
