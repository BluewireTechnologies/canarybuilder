using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.Console.NUnit3.Filesystem;
using Bluewire.Stash.Tool;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests.Tool
{
    [TestFixture]
    public class BackgroundGCTests
    {
        [Test]
        public async Task DoesNotRemoveLockedTransactions()
        {
            var stashRepository = new LocalStashRepository(Path.Combine(TemporaryDirectory.ForCurrentTest(), ".stashes"));
            using (var stash = await stashRepository.GetOrCreateExact(new VersionMarker("some-hash")))
            {
                await stash.Store(new MemoryStream(), "testfile");

                await new GarbageCollection(new VerboseLogger(TextWriter.Null, 0)).RunAndWait(stashRepository);

                await stash.Commit();

                var items = await stash.List().ToListAsync();
                Assert.That(items, Does.Contain("testfile"));
            }
        }

        [Test]
        public async Task DoesNotRemoveStashes()
        {
            var stashRepository = new LocalStashRepository(Path.Combine(TemporaryDirectory.ForCurrentTest(), ".stashes"));
            using (var stash = await stashRepository.GetOrCreateExact(new VersionMarker("some-hash")))
            {
                await stash.Store(new MemoryStream(), "testfile");
                await stash.Commit();
            }

            await new GarbageCollection(new VerboseLogger(TextWriter.Null, 0)).RunAndWait(stashRepository);

            using (var stash = await stashRepository.GetOrCreateExact(new VersionMarker("some-hash")))
            {
                var items = await stash.List().ToListAsync();
                Assert.That(items, Does.Contain("testfile"));
            }
        }
    }
}
