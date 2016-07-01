using System;
using System.Text.RegularExpressions;

namespace Bluewire.Common.GitWrapper.Parsing.Diff
{
    public static class DiffHelpers
    {
        public static LineType InterpretLineType(string line)
        {
            if (line.Length == 0) return LineType.Unknown;
            switch (line[0])
            {
                case 'd':
                    if (line.StartsWith("diff ")) return LineType.DiffHeader;
                    break;
                case 'i':
                    if (line.StartsWith("index ")) return LineType.IndexHeader;
                    break;
                case '@':
                    if (line.StartsWith("@@ ")) return LineType.ChunkHeader;
                    break;
                case '-':
                    if (line.StartsWith("--- ")) return LineType.OriginalPathHeader;
                    return LineType.DeleteLine;
                case '+':
                    if (line.StartsWith("+++ ")) return LineType.PathHeader;
                    return LineType.InsertLine;
                case ' ':
                    return LineType.ContextLine;
                case '\\':
                    if(line.StartsWith("\\ No newline at end of file")) return LineType.MissingNewLine;
                    break;
            }
            return LineType.Unknown;
        }


        public static string GetPathFromHeaderLine(string line)
        {
            // Strip the header prefix:
            var endOfPrefix = line.IndexOf(' ');
            if (endOfPrefix < 0) return null;
            // Strip the first segment of the path: 'a/' or 'b/' added by Git.
            var endOfPathPrefix = line.IndexOf('/', endOfPrefix);
            if (endOfPathPrefix < 0) return null;

            return line.Substring(endOfPathPrefix + 1);
        }

        private static readonly Regex rxChunkOffsetHeader = new Regex(@"^@+ \-(?<from>\d+)(,\d+)? \+(?<to>\d+)(,\d+)? @+(?<comment>.*)$");
        public static bool ParseChunkOffsets(string line, out int lineNumber, out int originalLineNumber)
        {
            lineNumber = -1;
            originalLineNumber = -1;
            var m = rxChunkOffsetHeader.Match(line);

            if (!m.Success) return false;
            if (!Int32.TryParse(m.Groups["from"].Value, out originalLineNumber)) return false;
            if (!Int32.TryParse(m.Groups["to"].Value, out lineNumber)) return false;
            return true;
        }
    }
}
