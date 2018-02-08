using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Async;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Common.GitWrapper.Parsing.Log
{
    public class GitLogReader : IDisposable
    {
        private readonly List<UnexpectedGitOutputFormatDetails> errors = new List<UnexpectedGitOutputFormatDetails>();
        public IEnumerable<UnexpectedGitOutputFormatDetails> Errors => errors;

        public LogEntry Current { get; private set; }

        private readonly LineReader reader;

        public GitLogReader(IAsyncEnumerator<string> lines)
        {
            reader = new LineReader(lines);
        }

        public async Task<bool> NextLogEntry()
        {
            if (reader.LineType == LineType.None)
            {
                if (!await reader.MoveNext()) return false;
            }

            Current = null;

            if (reader.LineType != LineType.Commit) return false;

            string commitHash;
            if (!TryParseHeader("commit ", reader.Current, out commitHash) || commitHash.Any(char.IsWhiteSpace))
            {
                errors.Add(new UnexpectedGitOutputFormatDetails { Line = reader.Current, Explanations = { "Malformed 'commit' header." } });
                return await DiscardUntil(LineType.Commit);
            }

            var entry = new LogEntry { Ref = new Ref(commitHash) };
            if (await TryReadLogEntry(entry))
            {
                Current = entry;
                return true;
            }

            Complete();
            return false;
        }

        private async Task<bool> TryReadLogEntry(LogEntry entry)
        {
            Debug.Assert(reader.LineType == LineType.Commit);
            if (!await reader.MoveNext()) return false;

            while (reader.LineType == LineType.NamedHeader)
            {
                string matched;
                if (TryParseHeader("Merge: ", reader.Current, out matched))
                {
                    if (entry.MergeParents == null) entry.MergeParents = matched.Split().Where(r => r.Length > 0).Select(r => new Ref(r)).ToArray();
                    else errors.Add(new UnexpectedGitOutputFormatDetails { Line = reader.Current, Explanations = { "Duplicate 'Merge:' header." } });
                }
                else if (TryParseHeader("Author: ", reader.Current, out matched))
                {
                    if (entry.Author == null) entry.Author = matched;
                    else errors.Add(new UnexpectedGitOutputFormatDetails { Line = reader.Current, Explanations = { "Duplicate 'Author:' header." } });
                }
                else if (TryParseHeader("Date: ", reader.Current, out matched))
                {
                    if (entry.Date == null) entry.Date = matched;
                    else errors.Add(new UnexpectedGitOutputFormatDetails { Line = reader.Current, Explanations = { "Duplicate 'Date:' header." } });
                }
                if (!await reader.MoveNext()) return false;
            }

            var message = new StringBuilder(256);
            while (reader.LineType != LineType.Commit)
            {
                switch (reader.LineType)
                {
                    case LineType.MessageLine:
                        message.AppendLine(ReadMessageLine(reader.Current));
                        break;
                    case LineType.Blank:
                        message.AppendLine();
                        break;
                    default:
                        errors.Add(new UnexpectedGitOutputFormatDetails { Line = reader.Current, Explanations = { $"Unexpected line type in message: {reader.LineType}" } });
                        message.AppendLine();
                        break;
                }
                if (!await reader.MoveNext()) break;
            }
            entry.Message = message.ToString();

            return true;
        }

        private void Complete()
        {
            Current = null;
        }

        public void Dispose()
        {
            Complete();
            reader.Dispose();
        }

        private static bool TryParseHeader(string header, string line, out string matched)
        {
            if (line.StartsWith(header))
            {
                matched = line.Substring(header.Length);
                return true;
            }
            matched = null;
            return false;
        }

        private static string ReadMessageLine(string line)
        {
            Debug.Assert(line.Length >= 4);
            Debug.Assert(String.IsNullOrWhiteSpace(line.Substring(0, 4)));
            return line.Substring(4);
        }

        private static LineType InterpretLineType(string line)
        {
            if (line == null) return LineType.Unknown;
            if (String.IsNullOrWhiteSpace(line)) return LineType.Blank;
            if (line.Length >= 4 && char.IsWhiteSpace(line[0]))
            {
                if (String.IsNullOrWhiteSpace(line.Substring(0, 4)))
                {
                    return LineType.MessageLine;
                }
                return LineType.Unknown;
            }
            if (line[0] == 'c')
            {
                if (line.StartsWith("commit ")) return LineType.Commit;
            }
            if (line.IndexOf(':') >= 0) return LineType.NamedHeader;
            return LineType.Unknown;
        }

        private async Task<bool> DiscardUntil(LineType type)
        {
            if (reader.LineType == type)
            {
                if (!await reader.MoveNext()) return false;
            }
            while (reader.LineType != type)
            {
                if (!await reader.MoveNext()) return false;
            }
            return true;
        }

        public class LineReader
        {
            private readonly IAsyncEnumerator<string> enumerator;

            public LineReader(IAsyncEnumerator<string> lines)
            {
                this.enumerator = lines;
            }

            public async Task<bool> MoveNext()
            {
                while (await enumerator.MoveNext())
                {
                    if (enumerator.Current.Length == 0) continue;
                    Current = enumerator.Current;
                    LineType = InterpretLineType(Current);
                    return true;
                }
                LineType = LineType.Unknown;
                return false;
            }

            public string Current { get; private set; }
            public LineType LineType { get; private set; }

            public void Dispose()
            {
                enumerator.Dispose();
            }
        }
    }
}
