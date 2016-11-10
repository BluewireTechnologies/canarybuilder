using System;
using System.Collections.Generic;

namespace Bluewire.Common.GitWrapper.Async
{
    public static class AsyncEnumeratorExtensions
    {
        public static IAsyncEnumerator<T> GetAsyncEnumerator<T>(this IObservable<T> observable)
        {
            return new AsyncBufferedEnumerator<T>(observable);
        }

        public static IAsyncEnumerator<T> GetAsyncEnumerator<T>(this IObservable<T> observable, int bufferLength)
        {
            return new AsyncBufferedEnumerator<T>(observable, bufferLength);
        }

        public static IAsyncEnumerator<T> GetAsyncEnumerator<T>(this IEnumerable<T> enumerable)
        {
            return new AsyncEnumeratorAdapter<T>(enumerable);
        }

        public static IAsyncEnumerator<T> ToAsync<T>(this IEnumerator<T> enumerator)
        {
            return new AsyncEnumeratorAdapter<T>(enumerator);
        }
    }
}
