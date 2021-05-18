using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Conventions;
using Bluewire.Stash.IntegrationTests.TestInfrastructure;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests
{
    [TestFixture]
    public class LocalStashRepositoryTests
    {
        [Test]
        public async Task TryGet_NonexistentVersion_ReturnsNull()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);
            var version = new VersionMarker("some-commit");

            var stash = await stashRepository.TryGet(version);
            Assert.That(stash, Is.Null);
        }

        [Test]
        public async Task CanGetOrCreate_WithCommitOnlyMarker()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);
            var version = new VersionMarker("some-commit");

            var stash = await CreateEmptyStash(stashRepository, version);
            Assert.That(stash.VersionMarker, Is.EqualTo(version).Using(VersionMarker.EqualityComparer));

            var fetched = await stashRepository.TryGet(version);
            Assert.That(fetched, Is.Not.Null);
        }

        [Test]
        public async Task CanGetOrCreate_WithVersionOnlyMarker()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);
            var version = new VersionMarker(SemanticVersion.FromString("20.21.10-beta"));

            var stash = await CreateEmptyStash(stashRepository, version);
            Assert.That(stash.VersionMarker, Is.EqualTo(version).Using(VersionMarker.EqualityComparer));

            var fetched = await stashRepository.TryGet(version);
            Assert.That(fetched, Is.Not.Null);
        }

        [Test]
        public async Task CanGetOrCreate_WithResolvedVersionMarker()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);
            var version = new VersionMarker(SemanticVersion.FromString("20.21.10-beta"), "some-commit");

            var stash = await CreateEmptyStash(stashRepository, version);
            Assert.That(stash.VersionMarker, Is.EqualTo(version).Using(VersionMarker.EqualityComparer));

            var fetched = await stashRepository.TryGet(version);
            Assert.That(fetched, Is.Not.Null);
        }

        [Test]
        public async Task CanTryGet_ResolvedVersionStash_WithCommitOnlyMarker()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);
            var version = new VersionMarker(SemanticVersion.FromString("20.21.10-beta"), "some-commit");

            var stash = await CreateEmptyStash(stashRepository, version);
            Assert.That(stash.VersionMarker, Is.EqualTo(version).Using(VersionMarker.EqualityComparer));

            var fetched = await stashRepository.TryGet(new VersionMarker("some-commit"));
            Assert.That(fetched, Is.Not.Null);
            Assert.That(fetched!.VersionMarker, Is.EqualTo(version).Using(VersionMarker.EqualityComparer));
        }

        [Test]
        public async Task CanTryGet_ResolvedVersionStash_WithVersionOnlyMarker()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);
            var version = new VersionMarker(SemanticVersion.FromString("20.21.10-beta"), "some-commit");

            var stash = await CreateEmptyStash(stashRepository, version);
            Assert.That(stash.VersionMarker, Is.EqualTo(version).Using(VersionMarker.EqualityComparer));

            var fetched = await stashRepository.TryGet(new VersionMarker(SemanticVersion.FromString("20.21.10-beta")));
            Assert.That(fetched, Is.Not.Null);
            Assert.That(fetched!.VersionMarker, Is.EqualTo(version).Using(VersionMarker.EqualityComparer));
        }

        [Test]
        public async Task CanListStashes()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            await CreateEmptyStash(stashRepository, new VersionMarker(SemanticVersion.FromString("20.21.1-beta"), "commit1"));
            await CreateEmptyStash(stashRepository, new VersionMarker(SemanticVersion.FromString("20.21.2-beta")));
            await CreateEmptyStash(stashRepository, new VersionMarker("commit3"));

            var list = await stashRepository.List().ToListAsync();

            Assert.That(list,
                Is.EqualTo(new []
                {
                    new VersionMarker(SemanticVersion.FromString("20.21.2-beta")),
                    new VersionMarker(SemanticVersion.FromString("20.21.1-beta"), "commit1"),
                    new VersionMarker("commit3"),
                }).Using(VersionMarker.EqualityComparer));
        }

        [Test]
        public async Task CanListStashesForMajorMinorVersion()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            await CreateEmptyStash(stashRepository, new VersionMarker(SemanticVersion.FromString("20.21.1-beta"), "commit1"));
            await CreateEmptyStash(stashRepository, new VersionMarker(SemanticVersion.FromString("20.20.2-beta")));
            await CreateEmptyStash(stashRepository, new VersionMarker(SemanticVersion.FromString("20.20.12-beta")));
            await CreateEmptyStash(stashRepository, new VersionMarker("commit3"));

            var list = await stashRepository.List(SemanticVersion.FromString("20.20.0"));

            Assert.That(list,
                Is.EqualTo(new []
                {
                    new VersionMarker(SemanticVersion.FromString("20.20.12-beta")),
                    new VersionMarker(SemanticVersion.FromString("20.20.2-beta")),
                }).Using(VersionMarker.EqualityComparer));
        }

        [Test]
        public async Task StashDoesNotExistUntilCommitted()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            var stash = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            await stash.Store(new MemoryStream(), "something");

            Assert.That(await stashRepository.List().ToListAsync(), Is.Empty);

            await stash.Commit();

            Assert.That(await stashRepository.List().ToListAsync(), Is.Not.Empty);
        }

        [Test]
        public async Task CanReadUncommittedStreamFromPendingStash()
        {
            var data = Encoding.UTF8.GetBytes("test");
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            var stash = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            await stash.Store(new MemoryStream(data), "something");

            var uncommitted = await stash.Get("something");
            Assert.That(uncommitted, Is.Not.Null);
            Assert.That(uncommitted!.ToArray(), Is.EqualTo(data));
        }

        [Test]
        public async Task CanReadCommittedStreamFromStash()
        {
            var data = Encoding.UTF8.GetBytes("test");
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            var stash = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            await stash.Store(new MemoryStream(data), "something");
            await stash.Commit();

            var committed = await stash.Get("something");
            Assert.That(committed, Is.Not.Null);
            Assert.That(committed!.ToArray(), Is.EqualTo(data));
        }

        [Test]
        public async Task CannotReadUncommittedStreamFromAnotherStashInstance()
        {
            var data = Encoding.UTF8.GetBytes("test");
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            var stash = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            await stash.Store(new MemoryStream(data), "something");

            var other = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            var uncommitted = await other.Get("something");
            Assert.That(uncommitted, Is.Null);
        }

        [Test]
        public async Task CanReadCommittedStreamFromAnotherStashInstance()
        {
            var data = Encoding.UTF8.GetBytes("test");
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            var stash = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            await stash.Store(new MemoryStream(data), "something");
            await stash.Commit();

            var other = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            var committed = await other.Get("something");
            Assert.That(committed, Is.Not.Null);
            Assert.That(committed!.ToArray(), Is.EqualTo(data));
        }

        [Test]
        public async Task CanListCommittedPaths()
        {
            var data = Encoding.UTF8.GetBytes("test");
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            var stash = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            await stash.Store(new MemoryStream(data), "something");
            await stash.Store(new MemoryStream(data), @"dir\something-else");
            await stash.Commit();

            var other = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            var committed = await other.List().ToListAsync();
            Assert.That(committed,
                Is.EquivalentTo(new []
                {
                    "something",
                    @"dir\something-else",
                }));
        }

        [Test]
        public async Task CannotListUncommittedPaths()
        {
            var data = Encoding.UTF8.GetBytes("test");
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            var stash = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            await stash.Store(new MemoryStream(data), "something");
            await stash.Commit();
            await stash.Store(new MemoryStream(data), @"dir\something-else");

            var other = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            var committed = await other.List().ToListAsync();
            Assert.That(committed,
                Is.EquivalentTo(new []
                {
                    "something",
                }));
        }

        [Test]
        public async Task CanDeleteStash()
        {
            var data = Encoding.UTF8.GetBytes("test");
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);

            var stash = await stashRepository.GetOrCreate(new VersionMarker("commit"));
            await stash.Store(new MemoryStream(data), "something");
            await stash.Commit();

            await stashRepository.Delete(new VersionMarker("commit"));

            var fetched = await stashRepository.TryGet(new VersionMarker("commit"));
            Assert.That(fetched, Is.Null);
        }

        private async Task<IStash> CreateEmptyStash(LocalStashRepository repository, VersionMarker marker)
        {
            var stash = await repository.GetOrCreate(marker);
            await stash.Commit();
            return stash;
        }
    }
}
