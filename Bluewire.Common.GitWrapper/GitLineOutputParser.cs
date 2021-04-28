using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper
{
    public abstract class GitLineOutputParser<T> : IGitAsyncOutputParser<T[]>
    {
        public virtual IEnumerable<UnexpectedGitOutputFormatDetails> Errors => Enumerable.Empty<UnexpectedGitOutputFormatDetails>();
        public abstract bool Parse(string line, out T entry);

        public async Task<T[]> Parse(IAsyncEnumerator<string> lines, CancellationToken token)
        {
            var results = new List<T>();
            while (await lines.MoveNextAsync())
            {
                T entry;
                if (Parse(lines.Current, out entry)) results.Add(entry);
            }
            return results.ToArray();
        }
    }
}
