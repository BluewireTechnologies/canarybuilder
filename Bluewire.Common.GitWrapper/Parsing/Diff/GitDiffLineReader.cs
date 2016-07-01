using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Async;

namespace Bluewire.Common.GitWrapper.Parsing.Diff
{
    public class GitDiffLineReader
    {
        private readonly IAsyncEnumerator<string> enumerator;

        public GitDiffLineReader(IAsyncEnumerator<string> lines)
        {
            this.enumerator = lines;
        }

        public async Task<bool> MoveNext()
        {
            while (await enumerator.MoveNext())
            {
                if(enumerator.Current.Length == 0) continue;
                Current = enumerator.Current;
                LineIndex++;
                LineType = DiffHelpers.InterpretLineType(Current);
                return true;
            }
            LineIndex = -1;
            LineType = LineType.Unknown;
            return false;
        }

        public long LineIndex { get; private set; } = -1;
        public string Current { get; private set; }
        public LineType LineType { get; private set; }
        
        public void Dispose()
        {
            enumerator.Dispose();
        }
    }
}
