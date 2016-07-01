using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;
using Bluewire.Common.GitWrapper.Async;
using Bluewire.Common.GitWrapper.Parsing.Diff;

namespace Bluewire.Common.GitWrapper.Parsing
{
    public interface IAsyncParser<TResult>
    {
        Task<TResult> Parse(IOutputPipe pipe, CancellationToken token);
    }

    public class GitDiffParser : IGitAsyncOutputParser<FileDiff[]>
    {
        public IEnumerable<UnexpectedGitOutputFormatDetails> Errors => Enumerable.Empty<UnexpectedGitOutputFormatDetails>();

        public async Task<FileDiff[]> Parse(IAsyncEnumerator<string> lines, CancellationToken token)
        {
            var diffs = new List<FileDiff>();
            var reader = new GitDiffReader(lines);
            while (await reader.NextFile())
            {
                var chunks = new List<DiffChunk>();

                var file = new FileDiff {
                    Path = reader.Path,
                    OriginalPath = reader.OriginalPath,
                    Chunks = chunks
                };
                while (await reader.NextChunk())
                {
                    var inserted = new List<DiffChunkLine>();
                    var deleted = new List<DiffChunkLine>();
                    while (await reader.NextLine())
                    {
                        if(reader.Line.Action == LineAction.Insert) inserted.Add(new DiffChunkLine { Line = reader.Line.NewNumber, Value = reader.Line.Text });
                        if(reader.Line.Action == LineAction.Delete) deleted.Add(new DiffChunkLine { Line = reader.Line.OldNumber, Value = reader.Line.Text });
                    }
                    chunks.Add(new DiffChunk { Inserted = inserted.AsReadOnly(), Deleted = deleted.AsReadOnly() });
                }
                diffs.Add(file);
            }
            return diffs.ToArray();
        }
    }

    public class FileDiff
    {
        public string Path { get; set; }
        public string OriginalPath { get; set; }
        public IEnumerable<DiffChunk> Chunks { get; set; }
    }

    public struct DiffChunk
    {
        public IEnumerable<DiffChunkLine> Inserted { get; set; }
        public IEnumerable<DiffChunkLine> Deleted { get; set; }
    }

    public struct DiffChunkLine
    {
        public int Line { get; set; }
        public string Value { get; set; }
    }
}
