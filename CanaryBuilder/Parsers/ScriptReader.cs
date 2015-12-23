using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CanaryBuilder.Common.Util;

namespace CanaryBuilder.Parsers
{
    public class ScriptReader
    {
        public IEnumerable<ScriptLine> EnumerateLines(TextReader reader)
        {
            var lineNumber = 0;
            foreach (var line in reader.ReadAllLines().Select(StripCommentsAndTrailingWhitespace))
            {
                lineNumber++;
                if (String.IsNullOrWhiteSpace(line)) continue;
                yield return new ScriptLine { Content = line, LineNumber = lineNumber };
            }
        }

        private string StripCommentsAndTrailingWhitespace(string line)
        {
            var commentIndex = line.IndexOf(COMMENT_MARKER);
            if (commentIndex < 0) return line.TrimEnd();
            return line.Substring(0, commentIndex).TrimEnd();
        }

        private const char COMMENT_MARKER = '#';
    }
}