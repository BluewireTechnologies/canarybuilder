using System;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper.Async
{
    public interface IAsyncEnumerator<out T> : IDisposable
    {
        Task<bool> MoveNext(CancellationToken token = default(CancellationToken));
        T Current { get; }
    }
}
