using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Conventions;
using NUnit.Framework;

namespace Bluewire.Stash.UnitTests
{
    [TestFixture]
    public class StashResolverTests
    {
        [Test]
        public async Task CanResolveInitialVersionStashForReleaseVersion_UsingTopology()
        {
            var initialStash = new MockStash();
            var stashRepository = new MockLocalStashRepository
            {
                Stashes =
                {
                    [new VersionMarker(SemanticVersion.FromString("20.21.0-beta"))] = initialStash,
                },
            };
            var commitTopology = new MockCommitTopology
            {
                Commits =
                {
                    new Commit { Hash = "last version", SemanticVersion = SemanticVersion.FromString("20.20.100-beta") },
                    new Commit { Hash = "initial", ParentHash = "last version", SemanticVersion = SemanticVersion.FromString("20.21.0-beta") },
                    new Commit { Hash = "update", ParentHash = "initial", SemanticVersion = SemanticVersion.FromString("20.21.1-beta") },
                    new Commit { Hash = "rc", ParentHash = "update", SemanticVersion = SemanticVersion.FromString("20.21.2-rc") },
                    new Commit { Hash = "release", ParentHash = "rc", SemanticVersion = SemanticVersion.FromString("20.21.3-release") },
                },
                LastVersions =
                {
                    ["20.20"] = new ResolvedVersionMarker(SemanticVersion.FromString("20.20.100-beta"), "last version"),
                },
            };

            var sut = new StashResolver(stashRepository);
            var version = await sut.FindClosestAncestor(commitTopology, new ResolvedVersionMarker(SemanticVersion.FromString("20.21.3-release"), "release"));

            Assert.That(version, Is.EqualTo(stashRepository.Stashes.Keys.Single()));
        }

        [Test]
        public async Task CanResolveExactMatchOfNonCanonicalVersion_UsingTopology()
        {
            var initialStash = new MockStash();
            var stashRepository = new MockLocalStashRepository
            {
                Stashes =
                {
                    [new VersionMarker(SemanticVersion.FromString("20.21.2-alpha.gsomething"), "alpha")] = initialStash,
                },
            };
            var commitTopology = new MockCommitTopology
            {
                Commits =
                {
                    new Commit { Hash = "last version", SemanticVersion = SemanticVersion.FromString("20.20.100-beta") },
                    new Commit { Hash = "initial", ParentHash = "last version", SemanticVersion = SemanticVersion.FromString("20.21.0-beta") },
                    new Commit { Hash = "update", ParentHash = "initial", SemanticVersion = SemanticVersion.FromString("20.21.1-beta") },
                    new Commit { Hash = "alpha", ParentHash = "update", SemanticVersion = SemanticVersion.FromString("20.21.2-alpha.gsomething") },
                },
                LastVersions =
                {
                    ["20.20"] = new ResolvedVersionMarker(SemanticVersion.FromString("20.20.100-beta"), "last version"),
                },
            };

            var sut = new StashResolver(stashRepository);
            var version = await sut.FindClosestAncestor(commitTopology, new ResolvedVersionMarker(SemanticVersion.FromString("20.21.2-alpha.gsomething"), "alpha"));

            Assert.That(version, Is.EqualTo(stashRepository.Stashes.Keys.Single()));
        }

        [Test]
        public async Task CanResolveInitialVersionStashForReleaseVersion_WithoutTopology()
        {
            var initialStash = new MockStash();
            var stashRepository = new MockLocalStashRepository
            {
                Stashes =
                {
                    [new VersionMarker(SemanticVersion.FromString("20.21.0-beta"))] = initialStash,
                },
            };

            var sut = new StashResolver(stashRepository);
            var version = await sut.FindClosestAncestor(new ResolvedVersionMarker(SemanticVersion.FromString("20.21.3-release"), "release"));

            Assert.That(version, Is.EqualTo(stashRepository.Stashes.Keys.Single()));
        }

        [Test]
        public async Task CanResolvePreviousInitialVersionStashForReleaseVersion_WithoutTopology()
        {
            var initialStash = new MockStash();
            var stashRepository = new MockLocalStashRepository
            {
                Stashes =
                {
                    [new VersionMarker(SemanticVersion.FromString("20.21.0-beta"))] = initialStash,
                },
            };

            var sut = new StashResolver(stashRepository);
            var version = await sut.FindClosestAncestor(new ResolvedVersionMarker(SemanticVersion.FromString("21.01.3-release"), "release"));

            Assert.That(version, Is.EqualTo(stashRepository.Stashes.Keys.Single()));
        }

        [Test]
        public async Task CanResolvePreviousInitialVersionStashForReleaseVersion_UsingTopology()
        {
            var initialStash = new MockStash();
            var stashRepository = new MockLocalStashRepository
            {
                Stashes =
                {
                    [new VersionMarker(SemanticVersion.FromString("20.21.0-beta"))] = initialStash,
                },
            };
            var commitTopology = new MockCommitTopology
            {
                Commits =
                {
                    new Commit { Hash = "last version", SemanticVersion = SemanticVersion.FromString("20.20.100-beta") },
                    new Commit { Hash = "initial", ParentHash = "last version", SemanticVersion = SemanticVersion.FromString("20.21.0-beta") },
                    new Commit { Hash = "update", ParentHash = "initial", SemanticVersion = SemanticVersion.FromString("20.21.1-beta") },
                    new Commit { Hash = "alpha", ParentHash = "update", SemanticVersion = SemanticVersion.FromString("20.21.2-alpha.gsomething") },
                    new Commit { Hash = "this-version", ParentHash = "update", SemanticVersion = SemanticVersion.FromString("21.01.0-beta") },
                    new Commit { Hash = "release", ParentHash = "this-version", SemanticVersion = SemanticVersion.FromString("21.01.3-release") },
                },
                LastVersions =
                {
                    ["20.20"] = new ResolvedVersionMarker(SemanticVersion.FromString("20.20.100-beta"), "last version"),
                },
            };

            var sut = new StashResolver(stashRepository);
            var version = await sut.FindClosestAncestor(commitTopology, new ResolvedVersionMarker(SemanticVersion.FromString("21.01.3-release"), "release"));

            Assert.That(version, Is.EqualTo(stashRepository.Stashes.Keys.Single()));
        }

        class MockStash : IStash
        {
            public VersionMarker VersionMarker => throw new NotImplementedException();
            public Task Store(Stream stream, string relativeTargetPath, CancellationToken token = default) => throw new NotImplementedException();
            public Task Commit(CancellationToken? token = null) => throw new NotImplementedException();
            public Task<Stream?> Get(string relativePath, CancellationToken token = default) => throw new NotImplementedException();
            public IAsyncEnumerable<string> List(CancellationToken token = default) => throw new NotImplementedException();
            public void Dispose() { }
        }

        class MockLocalStashRepository : IStashRepository
        {
            public IDictionary<VersionMarker, IStash> Stashes { get; } = new Dictionary<VersionMarker, IStash>(VersionMarker.EqualityComparer);

            public async IAsyncEnumerable<VersionMarker> List()
            {
                foreach (var version in Stashes.Keys.OrderByDescending(k => k.SemanticVersion, SemanticVersion.MajorMinorBuildComparer))
                {
                    yield return version;
                }
            }
        }

        class MockCommitTopology : ICommitTopology
        {
            public ICollection<Commit> Commits { get; } = new List<Commit>();
            public IDictionary<string, ResolvedVersionMarker> LastVersions { get; } = new Dictionary<string, ResolvedVersionMarker>();

            public async Task<ResolvedVersionMarker?> FullyResolve(VersionMarker marker)
            {
                if (marker.IsComplete) return marker.Checked;
                var resolved = Commits.FirstOrDefault(c => c.SemanticVersion == marker.SemanticVersion) ?? Commits.FirstOrDefault(c => c.Hash == marker.CommitHash);
                if (resolved == null) return null;
                return new ResolvedVersionMarker(resolved.SemanticVersion, resolved.Hash);
            }

            public async Task<bool> IsAncestor(ResolvedVersionMarker reference, ResolvedVersionMarker subject)
            {
                await foreach (var marker in EnumerateAncestry(reference))
                {
                    if (ResolvedVersionMarker.EqualityComparer.Equals(marker, subject)) return true;
                }
                return false;
            }

            public async IAsyncEnumerable<ResolvedVersionMarker> EnumerateAncestry(VersionMarker marker)
            {
                var commit = FindCommit(marker);
                if (commit == null) yield break;
                foreach (var result in EnumerateAncestry(commit))
                {
                    yield return GetMarker(result);
                }
            }

            public async Task<ResolvedVersionMarker?> GetLastVersionInMajorMinor(SemanticVersion semVer)
            {
                if (semVer == null) throw new ArgumentNullException(nameof(semVer));
                if (LastVersions.TryGetValue($"{semVer.Major}.{semVer.Minor}", out var value)) return value;
                return null;
            }

            private Commit? FindCommit(VersionMarker marker)
            {
                return Commits.FirstOrDefault(c => c.Hash == marker.CommitHash)
                    ?? Commits.FirstOrDefault(c => c.SemanticVersion.ToString() == marker.SemanticVersion?.ToString());
            }

            private ResolvedVersionMarker GetMarker(Commit commit) => new ResolvedVersionMarker(commit.SemanticVersion!, commit.Hash!);

            private IEnumerable<Commit> EnumerateAncestry(Commit commit)
            {
                var current = commit;
                while (current != null)
                {
                    yield return current;
                    current = Commits.FirstOrDefault(c => c.Hash == current.ParentHash);
                }
            }
        }

        class Commit
        {
            public SemanticVersion SemanticVersion { get; set; } = null!;
            public string Hash { get; set; } = null!;
            public string? ParentHash { get; set; }
        }
    }
}
