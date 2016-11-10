using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Common.GitWrapper.Async
{
    public class AsyncEnumeratorAdapter<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> underlying;

        public AsyncEnumeratorAdapter(IEnumerable<T> enumerable) : this(enumerable.GetEnumerator())
        {
        }

        /// <summary>
        /// Wrap the specified enumerator. Takes ownership of the enumerator, ie. it will be disposed
        /// when this AsyncEnumeratorAdapter is disposed.
        /// </summary>
        public AsyncEnumeratorAdapter(IEnumerator<T> underlying)
        {
            this.underlying = underlying;
        }

        public Task<bool> MoveNext(CancellationToken token = default(CancellationToken))
        {
            return underlying.MoveNext() ? ConstantTasks.True : ConstantTasks.False;
        }

        public T Current => underlying.Current;
        public void Dispose()
        {
            underlying.Dispose();
        }
    }
}
