using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Async;

namespace Bluewire.Common.GitWrapper
{
    public abstract class GitLineOutputParser<T> : IGitAsyncOutputParser<T[]>
    {
        public abstract IEnumerable<UnexpectedGitOutputFormatDetails> Errors { get; }
        public abstract bool Parse(string line, out T entry);

        public async Task<T[]> Parse(IAsyncEnumerator<string> lines, CancellationToken token)
        {
            var results = new List<T>();
            while (await lines.MoveNext(token))
            {
                T entry;
                if(Parse(lines.Current, out entry)) results.Add(entry);
            }
            return results.ToArray();
        }
    }
}
