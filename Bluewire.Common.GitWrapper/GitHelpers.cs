using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CliWrap;
using CliWrap.Buffered;

namespace Bluewire.Common.GitWrapper
{
    public static class GitHelpers
    {
        public static string ExpectOneLine(Command command, BufferedCommandResult result)
        {
            if (result.ExitCode != 0) throw new GitException(command, result.ExitCode, result.StandardError);
            using (var lineReader = new StringReader(result.StandardOutput))
            {
                var line = lineReader.ReadLine();
                if (line == null) throw new UnexpectedGitOutputFormatException(command, "No output.");

                var excess = ReadLines(lineReader).ToList();
                if (excess.Any()) throw new UnexpectedGitOutputFormatException(command, new UnexpectedGitOutputFormatDetails { Line = excess.First(), Explanations = { $"{excess.Count} excess lines." } });

                if (String.IsNullOrWhiteSpace(line)) throw new UnexpectedGitOutputFormatException(command, "Empty output.");
                return line;
            }
        }

        private static IEnumerable<string> ReadLines(TextReader reader)
        {
            var line = reader.ReadLine();
            while (line != null)
            {
                yield return line;
                line = reader.ReadLine();
            }
        }
    }
}
