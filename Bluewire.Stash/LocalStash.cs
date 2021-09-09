using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Bluewire.Stash
{
    /// <summary>
    /// Wraps a versioned stash stored on the local filesystem.
    /// </summary>
    /// <remarks>
    /// Instances of this class are not safe for concurrent usage.
    /// Distinct instances *may* be safe for concurrent usage.
    /// </remarks>
    public class LocalStash : IStash
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LocalStash));

        private readonly string finalPath;
        private readonly string tempPath;
        private readonly string lockPath;
        // Note that we never explicitly release this lock. Let the .NET GC worry about that for now.
        private IDisposable? tempPathLock;

        private string PendingPathOf(string relativeTargetPath) => Path.Combine(tempPath, relativeTargetPath);
        private string FinalPathOf(string relativeTargetPath) => Path.Combine(finalPath, relativeTargetPath);

        private readonly Queue<string> pendingStores = new Queue<string>();

        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();

        public LocalStash(string finalPath, string tempPath, string lockPath, VersionMarker marker)
        {
            if (finalPath == null) throw new ArgumentNullException(nameof(finalPath));
            this.finalPath = LocalFileSystem.ValidateFullPath(finalPath, nameof(finalPath));
            this.tempPath = LocalFileSystem.ValidateFullPath(tempPath, nameof(tempPath));
            this.lockPath = LocalFileSystem.ValidateFullPath(lockPath, nameof(lockPath));
            VersionMarker = marker;
        }

        public VersionMarker VersionMarker { get; }

        private async Task EnsureTempPath()
        {
            // Hold a lock on our temp path while it's in use. Prevents it from being cleaned up while in use.
            tempPathLock ??= await LocalFileSystem.AcquireTemporaryPath(tempPath, DefaultLockTimeout);
        }

        public async Task Store(Stream stream, string relativeTargetPath, CancellationToken token = default)
        {
            await EnsureTempPath();
            var fullPath = Path.Combine(tempPath, relativeTargetPath);
            using (var target = LocalFileSystem.CreateForExclusiveWrite(fullPath))
            {
                await stream.CopyToAsync(target);
            }
            pendingStores.Enqueue(relativeTargetPath);
        }

        private CancellationToken DefaultLockTimeout => new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;

        public async Task Commit(CancellationToken? token = null)
        {
            using (await LocalFileSystem.Lock(lockPath, token ?? DefaultLockTimeout))
            {
                var existing = pendingStores.Select(FinalPathOf).Where(LocalFileSystem.FileExists).ToArray();
                if (existing.Any()) throw new InvalidOperationException($"One or more files already exist in this stash: {string.Join(", ", existing)}");

                var preExisting = LocalFileSystem.DirectoryExists(finalPath);
                var writtenPaths = new List<string>();

                try
                {
                    // Create the stash directory now, since we're going to commit.
                    LocalFileSystem.EnsureDirectoryExists(finalPath);

                    while (pendingStores.Any())
                    {
                        var relativePath = pendingStores.Dequeue();
                        var source = PendingPathOf(relativePath);
                        var destination = FinalPathOf(relativePath);
                        Debug.Assert(!LocalFileSystem.FileExists(destination));
                        writtenPaths.Add(destination);
                        LocalFileSystem.Move(source, destination);
                    }
                }
                catch
                {
                    try
                    {
                        // Clean up while we still hold the lock.
                        pendingStores.Clear();
                        if (!preExisting)
                        {
                            LocalFileSystem.DeleteDirectoryTree(finalPath);
                        }
                        else
                        {
                            foreach (var path in writtenPaths)
                            {
                                LocalFileSystem.DeleteFile(path);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Unable to clean up stash after a commit failure: {VersionMarker}", ex);
                    }
                    // Rethrow the original failure only.
                    throw;
                }
            }
        }

        public async Task<Stream?> Get(string relativePath, CancellationToken token = default)
        {
            try
            {
                if (pendingStores.Contains(relativePath) && LocalFileSystem.FileExists(PendingPathOf(relativePath)))
                {
                    return LocalFileSystem.OpenForRead(PendingPathOf(relativePath));
                }
            }
            catch (FileNotFoundException)
            {
                // Ignore, and try the final path instead.
            }

            try
            {
                if (LocalFileSystem.FileExists(FinalPathOf(relativePath)))
                {
                    return LocalFileSystem.OpenForRead(FinalPathOf(relativePath));
                }
            }
            catch (FileNotFoundException)
            {
            }
            return null;
        }

        public IAsyncEnumerable<string> List(CancellationToken token = default)
        {
            return LocalFileSystem.EnumerateRelativePaths(finalPath, true);
        }

        public async Task Delete()
        {
            using (await LocalFileSystem.Lock(lockPath, DefaultLockTimeout))
            {
                pendingStores.Clear();
                LocalFileSystem.DeleteDirectoryTree(finalPath);
            }
        }

        public void Dispose()
        {
            tempPathLock?.Dispose();
            tempPathLock = null;
        }
    }
}
