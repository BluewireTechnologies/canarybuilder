using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash
{
    public interface IStash : IDisposable
    {
        VersionMarker VersionMarker { get; }
        /// <summary>
        /// Write a stream to a relative target path.
        /// This relative path will not be visible to other processes until after Commit is called.
        /// </summary>
        Task Store(Stream stream, string relativeTargetPath, CancellationToken token = default);
        /// <summary>
        /// Commit all pending Stores. If this fails, the stash instance should be discarded.
        /// </summary>
        /// <remarks>
        /// If there are no pending stores, this will just create an empty stash.
        /// If no CancellationToken is provided, the lock attempt will time out after 5 seconds.
        /// If a CancellationToken is provided and is never cancelled, the lock attempt will never time out.
        /// </remarks>
        Task Commit(CancellationToken? token = null);
        /// <summary>
        /// Get a stream from the specified relative path, if visible to this process.
        /// </summary>
        Task<Stream?> Get(string relativePath, CancellationToken token = default);
        /// <summary>
        /// List the relative paths committed to this stash.
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerable<string> List(CancellationToken token = default);
    }
}
