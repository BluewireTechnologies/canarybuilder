namespace CanaryBuilder.Parsers
{
    public class MissingParameterException : JobScriptException
    {
        public MissingParameterException(string message) : base(message)
        {
        }
    }
}