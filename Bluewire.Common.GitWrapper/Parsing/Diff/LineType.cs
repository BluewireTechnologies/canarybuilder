namespace Bluewire.Common.GitWrapper.Parsing.Diff
{
    public enum LineType
    {
        Unknown,
        DiffHeader,
        IndexHeader,
        OriginalPathHeader,
        PathHeader,
        ChunkHeader,
        
        ContextLine,
        InsertLine,
        DeleteLine,
        MissingNewLine
    }
}
