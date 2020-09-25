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
        private readonly LogDiffType logDiffType;
        private readonly List<UnexpectedGitOutputFormatDetails> errors = new List<UnexpectedGitOutputFormatDetails>();
        public IEnumerable<UnexpectedGitOutputFormatDetails> Errors => errors;

        public LogEntry Current { get; private set; }

        private readonly LineReader reader;

        public GitLogReader(IAsyncEnumerator<string> lines, LogDiffType logDiffType)
        {
            this.logDiffType = logDiffType;
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
            reader.LineMode = LineMode.Headers;

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

            var message = new List<string>(10);
            var files = new List<LogEntry.File>();
            while (reader.LineType != LineType.Commit)
            {
                switch (reader.LineType)
                {
                    case LineType.MessageLine:
                        message.Add(ReadMessageLine(reader.Current));
                        break;
                    case LineType.Blank:
                        reader.LineMode = LineMode.None;
                        break;
                    case LineType.Diff:
                        if (logDiffType == LogDiffType.Default) goto default;
                        files.Add(ReadDiffLine(reader.Current));
                        break;
                    default:
                        errors.Add(new UnexpectedGitOutputFormatDetails { Line = reader.Current, Explanations = { $"Unexpected line type in message: {reader.LineType}" } });
                        message.Add(Environment.NewLine);
                        break;
                }
                if (!await reader.MoveNext()) break;
            }
            entry.Message = GetMessageString(message);
            if (logDiffType == LogDiffType.NameOnly || logDiffType == LogDiffType.NameAndStatus)
            {
                entry.Files = files.ToArray();
            }

            return true;
        }

        private string GetMessageString(List<string> lines)
        {
            var builder = new StringBuilder(256);
            var i = 0;
            while (i < lines.Count)
            {
                builder.AppendLine(lines[i]);
                i++;
            }
            return builder.ToString();
        }

        private LogEntry.File ReadDiffLine(string line)
        {
            switch (logDiffType)
            {
                case LogDiffType.NameOnly:
                    return new LogEntry.File { Path = line };

                case LogDiffType.NameAndStatus:
                    var path = new string(line.Skip(1).SkipWhile(char.IsWhiteSpace).ToArray());
                    var entry = new LogEntry.File
                    {
                        Path = String.IsNullOrWhiteSpace(path) ? null : path,
                        IndexState = ParseIndexState(line[0]),
                    };
                    if (entry.IndexState == IndexState.Invalid)
                    {
                        errors.Add(new UnexpectedGitOutputFormatDetails { Line = reader.Current, Explanations = { "Unable to parse file status from line." } });
                    }
                    if (entry.Path == null)
                    {
                        errors.Add(new UnexpectedGitOutputFormatDetails { Line = reader.Current, Explanations = { "Unable to parse file path from line." } });
                    }
                    return entry;

                case LogDiffType.Default:
                default:
                    throw new NotSupportedException();
            }
        }

        private IndexState ParseIndexState(char flag)
        {
            switch (flag)
            {
                case ' ': return IndexState.Unmodified;
                case 'M': return IndexState.Modified;
                case 'A': return IndexState.Added;
                case 'D': return IndexState.Deleted;
                case 'R': return IndexState.Renamed;
                case 'C': return IndexState.Copied;
                case 'U': return IndexState.UpdatedButUnmerged;
                case '?': return IndexState.Untracked;
                case '!': return IndexState.Ignored;
            }
            return IndexState.Invalid;
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

        private static LineType InterpretLineType(string line, LineMode mode)
        {
            if (line == null) return LineType.Unknown;
            if (String.IsNullOrEmpty(line)) return LineType.Blank;
            switch (mode)
            {
                case LineMode.None:
                    if (line[0] == 'c')
                    {
                        if (line.StartsWith("commit ")) return LineType.Commit;
                    }
                    if (IsMessageLine(line)) return LineType.MessageLine;
                    return LineType.Diff;

                case LineMode.Headers:
                    if (line.IndexOf(':') >= 0) return LineType.NamedHeader;
                    break;

                case LineMode.Body:
                    if (IsMessageLine(line)) return LineType.MessageLine;
                    break;
            }
            return LineType.Unknown;
        }

        private static bool IsMessageLine(string line)
        {
            if (line.Length >= 4 && char.IsWhiteSpace(line[0]))
            {
                if (String.IsNullOrWhiteSpace(line.Substring(0, 4)))
                {
                    return true;
                }
            }
            return false;
        }

        enum LineMode
        {
            None,
            Headers,
            Body
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

        class LineReader
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
                    Current = enumerator.Current;
                    LineType = InterpretLineType(Current, LineMode);
                    return true;
                }
                LineType = LineType.Unknown;
                return false;
            }

            public string Current { get; private set; }
            public LineType LineType { get; private set; }
            public LineMode LineMode { get; set; }

            public void Dispose()
            {
                enumerator.Dispose();
            }
        }
    }
}
