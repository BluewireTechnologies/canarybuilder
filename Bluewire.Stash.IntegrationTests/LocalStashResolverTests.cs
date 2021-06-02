using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Conventions;
using NUnit.Framework;

namespace Bluewire.Stash.IntegrationTests
{
    [TestFixture]
    public class LocalStashResolverTests
    {
        [Test]
        public async Task CanResolveInitialVersionStashForReleaseVersion_UsingTopology()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);
            var initial = new VersionMarker(SemanticVersion.FromString("20.21.0-beta"));
            var stash = await stashRepository.GetOrCreate(initial);
            await stash.Commit();

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

            var version = await stashRepository.FindClosestAncestor(commitTopology, new ResolvedVersionMarker(SemanticVersion.FromString("20.21.3-release"), "release"));

            Assert.That(version?.VersionMarker, Is.EqualTo(initial).Using(VersionMarker.EqualityComparer));
        }

        [Test]
        public async Task CanResolveInitialVersionStashForReleaseVersion_WithoutTopology()
        {
            var stashRepository = new LocalStashRepository(Default.TemporaryDirectory);
            var initial = new VersionMarker(SemanticVersion.FromString("20.21.0-beta"));
            var stash = await stashRepository.GetOrCreate(initial);
            await stash.Commit();

            var version = await stashRepository.FindClosestAncestor(new ResolvedVersionMarker(SemanticVersion.FromString("20.21.3-release"), "release"));

            Assert.That(version?.VersionMarker, Is.EqualTo(initial).Using(VersionMarker.EqualityComparer));
        }

        class MockCommitTopology : ICommitTopology
        {
            public ICollection<Commit> Commits { get; } = new List<Commit>();
            public IDictionary<string, ResolvedVersionMarker> LastVersions { get; } = new Dictionary<string, ResolvedVersionMarker>();

            public async Task<ResolvedVersionMarker?> FullyResolve(VersionMarker marker)
            {
                var commit = FindCommit(marker);
                if (commit == null) return null;
                return GetMarker(commit);
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
