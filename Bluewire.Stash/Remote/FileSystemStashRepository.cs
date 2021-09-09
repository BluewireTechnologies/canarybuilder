using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.Remote
{
    public class FileSystemStashRepository : IRemoteStashRepository
    {
        public LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();

        private readonly string path;
        private readonly string tempPath;

        private string GetTransactionPath(Guid txId) => Path.Combine(tempPath, txId.ToString("D"));
        private string GetEntryPath(VersionMarker entry) => Path.Combine(path, MapToDirectoryName(entry));

        public FileSystemStashRepository(string path, string tempPath)
        {
            this.path = path;
            this.tempPath = tempPath;
        }

        private CancellationToken DefaultLockTimeout => new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

        public async Task Push(Guid txId, string relativePath, Stream stream, CancellationToken token = default)
        {
            using (await LocalFileSystem.AcquireTemporaryPath(tempPath, DefaultLockTimeout))
            {
                var targetPath = Path.Combine(GetTransactionPath(txId), LocalFileSystem.ValidateRelativePath(relativePath, nameof(relativePath)));
                using (var destination = LocalFileSystem.CreateForExclusiveWrite(targetPath))
                {
                    await stream.CopyToAsync(destination);
                }
            }
        }

        public Task Commit(VersionMarker entry, Guid txId, CancellationToken token = default)
        {
            LocalFileSystem.MoveDirectory(GetTransactionPath(txId), GetEntryPath(entry));
            return Task.CompletedTask;
        }

        public async Task<Stream> Pull(VersionMarker entry, string relativePath, CancellationToken token = default)
        {
            var sourcePath = Path.Combine(GetEntryPath(entry), LocalFileSystem.ValidateRelativePath(relativePath, nameof(relativePath)));
            return LocalFileSystem.OpenForRead(sourcePath);
        }

        public async IAsyncEnumerable<VersionMarker> List([EnumeratorCancellation] CancellationToken token = default)
        {
            var root = Directory.CreateDirectory(path);
            foreach (var match in root.EnumerateDirectories("*", SearchOption.TopDirectoryOnly))
            {
                token.ThrowIfCancellationRequested();
                var entry = MapFromDirectoryName(match.Name);
                if (entry != null) yield return entry.Value;
            }
        }

        public IAsyncEnumerable<string> ListFiles(VersionMarker entry, CancellationToken token = default)
        {
            var queryPath = GetEntryPath(entry);
            return LocalFileSystem.EnumerateRelativePaths(queryPath, true);
        }

        public async Task<bool> Exists(VersionMarker entry, CancellationToken token = default)
        {
            var queryPath = GetEntryPath(entry);
            return LocalFileSystem.DirectoryExists(queryPath);
        }

        private string MapToDirectoryName(VersionMarker entry)
        {
            return VersionMarkerStringConverter.ForDirectoryName().ToString(entry);
        }

        private VersionMarker? MapFromDirectoryName(string name)
        {
            if (!VersionMarkerStringConverter.ForDirectoryName().TryParse(name, out var value)) return null;
            return value;
        }
    }
}
