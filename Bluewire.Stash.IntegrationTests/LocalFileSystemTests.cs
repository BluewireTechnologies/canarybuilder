using NUnit.Framework;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Bluewire.Stash.IntegrationTests
{
    [TestFixture]
    public class LocalFileSystemTests
    {
        [Test]
        public async Task CanAcquireLock()
        {
            var file = Path.Combine(Default.TemporaryDirectory, "lockfile");
            using (await new LocalFileSystem().Lock(file))
            {
                Assert.That(File.Exists(file));
            }
        }

        [Test]
        public async Task CannotAcquireLockIfFileAlreadyExists()
        {
            var file = Path.Combine(Default.TemporaryDirectory, "lockfile");

            Directory.CreateDirectory(Default.TemporaryDirectory);
            File.WriteAllBytes(file, new byte[0]);

            var cts = new CancellationTokenSource(1000);
            Assert.That(async () =>
            {
                using (await new LocalFileSystem().Lock(file, cts.Token))
                {
                }
            }, Throws.InstanceOf<OperationCanceledException>());
        }

        [Test]
        public async Task CannotAcquireOverlappingLocks()
        {
            var file = Path.Combine(Default.TemporaryDirectory, "lockfile");

            var cts = new CancellationTokenSource(1000);
            using (await new LocalFileSystem().Lock(file, default))
            {
                Assert.That(() => new LocalFileSystem().Lock(file, cts.Token), Throws.InstanceOf<OperationCanceledException>());
            }
        }

        [Test]
        public async Task CannotDeleteTemporaryDirectoryInUse()
        {
            var cts = new CancellationTokenSource(1000);
            var tempPath = Path.Combine(Default.TemporaryDirectory, "temp");

            using (await new LocalFileSystem().AcquireTemporaryPath(tempPath, cts.Token))
            {
                Assert.That(await new LocalFileSystem().TryDeleteTemporaryPath(tempPath), Is.False);
            }
        }

        [Test]
        public async Task CanDeleteTemporaryDirectoryNotInUse()
        {
            var cts = new CancellationTokenSource(1000);
            var tempPath = Path.Combine(Default.TemporaryDirectory, "temp");

            using (await new LocalFileSystem().AcquireTemporaryPath(tempPath, cts.Token))
            {
            }
            Assert.That(await new LocalFileSystem().TryDeleteTemporaryPath(tempPath), Is.True);
        }

        [Test]
        public async Task DeletingNonexistentTemporaryDirectoryReturnsTrue()
        {
            var cts = new CancellationTokenSource(1000);
            var tempPath = Path.Combine(Default.TemporaryDirectory, "temp");

            Assert.That(await new LocalFileSystem().TryDeleteTemporaryPath(tempPath), Is.True);
        }
    }
}
