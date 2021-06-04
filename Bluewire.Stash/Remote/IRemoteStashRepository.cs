using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.Remote
{
    public interface IRemoteStashRepository
    {
        Task Push(Guid txId, string relativePath, Stream stream, CancellationToken token = default);
        Task Commit(VersionMarker entry, Guid txId, CancellationToken token = default);
        Task<Stream> Pull(VersionMarker entry, string relativePath, CancellationToken token = default);
        IAsyncEnumerable<VersionMarker> List(CancellationToken token = default);
        IAsyncEnumerable<string> ListFiles(VersionMarker entry, CancellationToken token = default);
        Task<bool> Exists(VersionMarker entry, CancellationToken token = default);
    }
}
