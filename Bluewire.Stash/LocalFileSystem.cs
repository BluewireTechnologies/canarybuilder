using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Bluewire.Stash
{
    public class LocalFileSystem
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LocalFileSystem));

        public virtual string ValidateFullPath(string path, string argumentName)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Value cannot be null or whitespace.", argumentName);
            if (path.Intersect(Path.GetInvalidPathChars()).Any()) throw new ArgumentException($"Path contains invalid characters: {path}", argumentName);
            if (!Path.IsPathRooted(path)) throw new ArgumentException($"Not an absolute path: {path}", argumentName);
            return path;
        }

        public virtual bool FileExists(string fullPath) =>
            File.Exists(ValidateFullPath(fullPath, nameof(fullPath)));

        public virtual bool DirectoryExists(string fullPath) =>
            Directory.Exists(ValidateFullPath(fullPath, nameof(fullPath)));

        public virtual void EnsureDirectoryExists(string fullPath) =>
            Directory.CreateDirectory(ValidateFullPath(fullPath, nameof(fullPath)));

        private static void EnsureContainingDirectoryExists(string fullFilePath)
        {
            var containingDirectory = Path.GetDirectoryName(fullFilePath);
            Directory.CreateDirectory(containingDirectory!);
        }

        public virtual void DeleteDirectoryTree(string fullPath) =>
            Directory.Delete(ValidateFullPath(fullPath, nameof(fullPath)), true);

        public virtual void DeleteFile(string fullPath) =>
            File.Delete(ValidateFullPath(fullPath, nameof(fullPath)));

        public virtual Stream OpenForRead(string fullPath) =>
            File.OpenRead(ValidateFullPath(fullPath, nameof(fullPath)));

        public virtual Stream CreateForExclusiveWrite(string fullPath)
        {
            var destination = ValidateFullPath(fullPath, nameof(fullPath));
            EnsureContainingDirectoryExists(destination);
            return new FileStream(destination, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096);
        }

        public virtual void Move(string sourceFullPath, string destinationFullPath)
        {
            var source = ValidateFullPath(sourceFullPath, nameof(sourceFullPath));
            var destination = ValidateFullPath(destinationFullPath, nameof(destinationFullPath));
            EnsureContainingDirectoryExists(destination);
            File.Move(source, destination);
        }

        public virtual async Task<IDisposable> AcquireTemporaryPath(string tempPath, CancellationToken cancellationToken = default)
        {
            // Acquire a lockfile inside the temporary path. Prevents clean-up of paths in use.
            var lockFilePath = Path.Combine(ValidateFullPath(tempPath, nameof(tempPath)), ".lock");
            return await Lock(lockFilePath, cancellationToken);
        }

        /// <summary>
        /// Try to delete the specified temporary path.
        /// </summary>
        /// <returns>True if the path was successfully deleted, false if the path is still locked. Throws on any other failure.</returns>
        public virtual async Task<bool> TryDeleteTemporaryPath(string tempPath)
        {
            var tempFullPath = ValidateFullPath(tempPath, nameof(tempPath));
            try
            {
                if (!Directory.Exists(tempFullPath)) return true;
                var lockFilePath = Path.Combine(tempFullPath, ".lock");
                // Try to delete the lock file first. This will fail if it's still in use.
                File.Delete(lockFilePath);
            }
            catch (IOException ex) when ((uint)ex.HResult == 0x80070020)
            {
                return false;
            }
            Directory.Delete(tempFullPath, true);
            return true;
        }

        public virtual async Task<IDisposable> Lock(string lockFilePath, CancellationToken cancellationToken = default)
        {
            var lockFile = new LockFile(ValidateFullPath(lockFilePath, nameof(lockFilePath)));
            try
            {
                await lockFile.Acquire(cancellationToken);
                return lockFile;
            }
            catch
            {
                lockFile.Dispose();
                throw;
            }
        }

        class LockFile : IDisposable
        {
            private readonly string lockPath;
            private FileStream? instance;
            private bool released;

            public LockFile(string lockPath)
            {
                this.lockPath = lockPath;
            }

            public async Task Acquire(CancellationToken token)
            {
                // Always try at least once to acquire the lock. Allows for zero-timeout acquisition by providing a cancelled token.
                do
                {
                    if (instance != null) throw new InvalidOperationException();
                    try
                    {
                        EnsureContainingDirectoryExists(lockPath);
                        instance = new FileStream(lockPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
                        return;
                    }
                    catch (Exception ex)
                    {
                        log.Debug($"Unable to acquire lock: {lockPath}", ex);
                    }
                    await Task.Delay(100, token);
                }
                while (!token.IsCancellationRequested);
            }

            public void Dispose()
            {
                if (released) return;
                if (instance == null) return;
                // Best effort clean-up only.
                try
                {
                    released = true;
                    instance?.Dispose();
                }
                catch (Exception ex)
                {
                    log.Error($"Unable to clean up lock: {lockPath}", ex);
                }
            }
        }

        public async IAsyncEnumerable<string> EnumerateAbsolutePaths(string rootPath)
        {
            // Could probably accept a glob argument as well in future, and use FlexiGlob for walking the hierarchy.

            var container = new DirectoryInfo(ValidateFullPath(rootPath, nameof(rootPath)));
            if (!container.Exists) yield break;
            foreach (var file in container.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                yield return file.FullName;
            }
        }

        public async IAsyncEnumerable<string> EnumerateRelativePaths(string rootPath)
        {
            // Could probably accept a glob argument as well in future, and use FlexiGlob for walking the hierarchy.

            var container = new DirectoryInfo(ValidateFullPath(rootPath, nameof(rootPath)));
            if (!container.Exists) yield break;
            foreach (var file in container.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                yield return GetRelativePath(file, container);
            }
        }

        private string GetRelativePath(FileSystemInfo item, DirectoryInfo root)
        {
            var parts = new Stack<string>();
            FileSystemInfo? current = item;
            while (current != null && current.FullName.Length > root.FullName.Length)
            {
                parts.Push(current.Name);
                current = GetParent(current);
            }
            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);

            static FileSystemInfo? GetParent(FileSystemInfo node)
            {
                if (node is FileInfo file) return file.Directory;
                if (node is DirectoryInfo directory) return directory.Parent;
                // Can't happen?
                throw new NotSupportedException(node.GetType().FullName);
            }
        }
    }
}
