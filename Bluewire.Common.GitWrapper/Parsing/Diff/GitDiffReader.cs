using System;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Async;

namespace Bluewire.Common.GitWrapper.Parsing.Diff
{
    public class GitDiffReader
    {
        private readonly GitDiffLineReader reader;

        public GitDiffReader(IAsyncEnumerator<string> lines)
        {
            this.reader = new GitDiffLineReader(lines);
            currentFile = new DiffFile { DiffHeaderLineNumber = reader.LineIndex };
        }

        public async Task<bool> NextFile()
        {
            if (currentFile == null) return false;
            if (currentFile.DiffHeaderLineNumber < reader.LineIndex)
            {
                if (await TryReadDiffHeader()) return true;
            }

            while (await reader.MoveNext())
            {
                if (await TryReadDiffHeader()) return true;
            }
            Complete();
            return false;
        }

        private async Task<bool> TryReadDiffHeader()
        {
            if (reader.LineType != LineType.DiffHeader) return false;

            var file = new DiffFile { DiffHeaderLineNumber = reader.LineIndex };

            while (await reader.MoveNext())
            {
                if (reader.LineType == LineType.DiffHeader) break; // No chunk?!
                if (reader.LineType == LineType.ChunkHeader) break;

                switch (reader.LineType)
                {
                    case LineType.Unknown:
                    case LineType.IndexHeader:
                        break;

                    case LineType.OriginalPathHeader:
                        file.OriginalPath = DiffHelpers.GetPathFromHeaderLine(reader.Current);
                        break;

                    case LineType.PathHeader:
                        file.Path = DiffHelpers.GetPathFromHeaderLine(reader.Current);
                        break;

                    case LineType.ChunkHeader:
                    case LineType.ContextLine:
                    case LineType.InsertLine:
                    case LineType.DeleteLine:
                    default:
                        throw new FormatException($"Malformed diff, reading line {reader.LineIndex}");
                }
            }
            currentFile = file;
            return true;
        }

        private bool TryReadChunkHeader(DiffFile file)
        {
            if (reader.LineType != LineType.ChunkHeader) return false;
            file.ChunkHeaderLineNumber = reader.LineIndex;
            file.ChunkCompleted = false;

            int lineNumber;
            int originalLineNumber;
            if (!DiffHelpers.ParseChunkOffsets(reader.Current, out lineNumber, out originalLineNumber))
            {
                throw new FormatException($"Malformed chunk header, reading line {reader.LineIndex}");
            }
            file.LineNumber = lineNumber;
            file.OriginalLineNumber = originalLineNumber;
            return true;
        }

        public async Task<bool> NextChunk()
        {
            if (currentFile == null) return false;
            if (currentFile.ChunkHeaderLineNumber < reader.LineIndex)
            {
                if (TryReadChunkHeader(currentFile)) return true;
            }

            // Reached a new file. Must call NextFile() before iterating its chunks.
            if (reader.LineType == LineType.DiffHeader) return false;

            while (await reader.MoveNext())
            {
                if (await TryReadDiffHeader()) return true;
            }
            Complete();
            return false;
        }

        public async Task<bool> NextLine()
        {
            if (currentFile == null) return false;
            if (currentFile.ChunkCompleted) return false;
            if (!await reader.MoveNext())
            {
                Complete();
                return false;
            }

            switch (reader.LineType)
            {
                case LineType.DiffHeader:
                    currentFile.ChunkCompleted = true;
                    return false;
                case LineType.ChunkHeader:
                    currentFile.ChunkCompleted = true;
                    return false;

                case LineType.ContextLine:
                    Line = new DiffLine { Action = LineAction.Context, Text = reader.Current.Substring(1), OldNumber = currentFile.OriginalLineNumber, NewNumber = currentFile.LineNumber };
                    currentFile.LineNumber++;
                    currentFile.OriginalLineNumber++;
                    return true;

                case LineType.InsertLine:
                    Line = new DiffLine { Action = LineAction.Insert, Text = reader.Current.Substring(1), NewNumber = currentFile.LineNumber };
                    currentFile.LineNumber++;
                    return true;

                case LineType.DeleteLine:
                    Line = new DiffLine { Action = LineAction.Delete, Text = reader.Current.Substring(1), OldNumber = currentFile.OriginalLineNumber };
                    currentFile.OriginalLineNumber++;
                    return true;

                case LineType.MissingNewLine:
                    Line = new DiffLine { Action = LineAction.MissingNewLine, Text = reader.Current.Substring(1), OldNumber = currentFile.OriginalLineNumber, NewNumber = currentFile.LineNumber };
                    currentFile.LineNumber++;
                    return true;

                case LineType.Unknown:
                case LineType.IndexHeader:
                case LineType.OriginalPathHeader:
                case LineType.PathHeader:
                default:
                    throw new FormatException($"Malformed diff, reading line {reader.LineIndex}");
            }
        }

        private void Complete()
        {
            currentFile = null;
            Line = default(DiffLine);
        }

        private DiffFile currentFile;

        public string Path => currentFile?.Path;
        public string OriginalPath => currentFile?.OriginalPath;

        public DiffLine Line { get; private set; }

        class DiffFile
        {
            public long DiffHeaderLineNumber { get; set; }
            public string Path { get; set; }
            public string OriginalPath { get; set; }

            public long ChunkHeaderLineNumber { get; set; }
            public bool ChunkCompleted { get; set; }
            public int LineNumber { get; set; }
            public int OriginalLineNumber { get; set; }
        }

        public struct DiffLine
        {
            public LineAction Action { get; set; }
            public int OldNumber { get; set; }
            public int NewNumber { get; set; }
            public string Text { get; set; }

            public override string ToString()
            {
                return DiffLineFormatter.Default.Format(this);
            }
        }
    }
}
