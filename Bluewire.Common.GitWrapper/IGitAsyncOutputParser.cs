using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Async;

namespace Bluewire.Common.GitWrapper
{
    public interface IGitAsyncOutputParser<T>
    {
        IEnumerable<UnexpectedGitOutputFormatDetails> Errors { get; }
        Task<T> Parse(IAsyncEnumerator<string> lines, CancellationToken token);
    }
}
