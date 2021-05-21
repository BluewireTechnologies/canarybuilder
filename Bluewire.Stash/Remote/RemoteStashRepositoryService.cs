using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.Remote
{
    public class RemoteStashRepositoryService
    {
        private readonly string rootPath;
        public LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        public Func<DateTimeOffset> Now = () => DateTimeOffset.Now;

        public RemoteStashRepositoryService(string rootPath)
        {
            if (rootPath == null) throw new ArgumentNullException(nameof(rootPath));
            if (!Path.IsPathRooted(rootPath)) throw new ArgumentException($"Not an absolute path: {rootPath}", nameof(rootPath));
            this.rootPath = rootPath;
        }

        private string GetTempPath() => Path.Combine(rootPath, ".tx");

        public IRemoteStashRepository GetNamed(string name)
        {
            var path = Path.Combine(rootPath, name);
            var tempPath = Path.Combine(GetTempPath(), name);
            return new FileSystemStashRepository(path, tempPath) { LocalFileSystem = LocalFileSystem };
        }

        public async IAsyncEnumerable<string> CleanUpTemporaryObjects(IBlobCleaner blobCleaner, [EnumeratorCancellation] CancellationToken token)
        {
            var maxAge = TimeSpan.FromHours(4);
            await foreach (var path in LocalFileSystem.EnumerateAbsolutePaths(GetTempPath()).WithCancellation(token))
            {
                var info = await LocalFileSystem.GetInfo(path);
                if (info == null) continue;

                var age = Now() - info.CreationTime;
                if (age < maxAge) continue;

                if (await TryCleanUp(blobCleaner, path))
                {
                    yield return path;
                }
            }
        }

        [DebuggerNonUserCode]
        private async Task<bool> TryCleanUp(IBlobCleaner blobCleaner, string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    // Lock file? Try to delete.
                    File.Delete(tempPath);
                    return true;
                }
                if (Directory.Exists(tempPath))
                {
                    // Pending transaction. Clean up referenced blobs before deleting files.
                    var anyFailures = false;
                    await foreach (var subPath in LocalFileSystem.EnumerateAbsolutePaths(tempPath))
                    {
                        if (!LocalFileSystem.FileExists(subPath)) continue;    // Directory.
                        if (await blobCleaner.TryCleanUp(LocalFileSystem, subPath))
                        {
                            LocalFileSystem.DeleteFile(subPath);
                        }
                        else
                        {
                            anyFailures = true;
                        }
                    }
                    if (anyFailures) return false;
                    return await LocalFileSystem.TryDeleteTemporaryPath(tempPath);
                }
            }
            catch
            {
                // Ignore failures.
            }
            return false;
        }
    }
}
