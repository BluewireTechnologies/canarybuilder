namespace Bluewire.Common.GitWrapper.Parsing.Log
{
    public enum LineType
    {
        None,   // No line loaded, ie. not started reading yet.
        Unknown,
        Commit,
        NamedHeader,
        Blank,
        MessageLine,
        Diff,
    }
}
