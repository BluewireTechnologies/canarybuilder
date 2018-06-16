namespace CanaryBuilder.Parsers
{
    public class DuplicateDirectiveException : JobScriptSyntaxErrorException
    {
        public DuplicateDirectiveException(ScriptLine line, string directive) : base(line, $"The directive '{directive}' was encountered multiple times.")
        {
        }
    }
}
