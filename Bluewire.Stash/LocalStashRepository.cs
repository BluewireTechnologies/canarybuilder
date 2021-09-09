using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Conventions;
using log4net;

namespace Bluewire.Stash
{
    public class LocalStashRepository : ILocalStashRepository
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(LocalStashRepository));

        private readonly string rootPath;

        internal LocalFileSystem LocalFileSystem { get; set; } = new LocalFileSystem();
        private string StashTemporaryDirectory => Path.Combine(rootPath, ".tx-pending");

        public LocalStashRepository(string rootPath)
        {
            if (rootPath == null) throw new ArgumentNullException(nameof(rootPath));
            if (!Path.IsPathRooted(rootPath)) throw new ArgumentException($"Not an absolute path: {rootPath}", nameof(rootPath));
            this.rootPath = rootPath;
        }

        private DirectoryInfo EnsureRootPathExists()
        {
            return Directory.CreateDirectory(rootPath);
        }

        public async Task<IStash?> FindClosestAncestor(VersionMarker marker)
        {
            var resolver = new StashResolver(this);
            var stashMarker = await resolver.FindClosestAncestor(marker);
            if (!stashMarker.IsValid) return null;
            return await TryGet(stashMarker);
        }

        public async Task<IStash?> FindClosestAncestor(ICommitTopology topology, VersionMarker marker)
        {
            var resolver = new StashResolver(this);
            var resolved = await topology.FullyResolve(marker);
            if (resolved == null) return null;  // Commit cannot be resolved?

            var stashMarker = await resolver.FindClosestAncestor(topology, resolved.Value);
            if (!stashMarker.IsValid) return null;
            return await TryGet(stashMarker);
        }

        public Task<IStash> GetOrCreateExact(VersionMarker marker) => GetOrCreateInternal(marker, true);
        public Task<IStash> GetOrCreate(VersionMarker marker) => GetOrCreateInternal(marker, false);

        private async Task<IStash> GetOrCreateInternal(VersionMarker marker, bool exact)
        {
            var existing = await TryGet(marker);
            if (existing != null)
            {
                if (!exact) return existing;
                if (VersionMarker.EqualityComparer.Equals(existing.VersionMarker, marker)) return existing;
            }
            try
            {
                var name = new DirectoryNameVersionMarkerMapping().MapToDirectoryName(marker);
                EnsureRootPathExists();
                // Must not create the stash directory yet, since nothing has been committed to it.
                return GetStashWithCheckedName(marker, name);
            }
            catch
            {
                // Try looking for an existing stash again.
                var secondTry = await TryGet(marker);
                if (secondTry != null) return secondTry;
                throw;
            }
        }

        public async Task<IStash?> TryGet(VersionMarker marker)
        {
            // Favour exact match on both hash and version first, then on hash, then just on version.
            return FindExactMatch(marker)
                .Concat(FindByHash(marker))
                .Concat(FindByVersion(marker))
                .FirstOrDefault();
        }

        /// <summary>
        /// Find all stashes matching the provided commit hash.
        /// </summary>
        private IEnumerable<IStash> FindExactMatch(VersionMarker marker)
        {
            if (!marker.IsComplete) yield break;
            foreach (var stash in EnumerateStashesMatching(new DirectoryNameVersionMarkerMapping().GetPatternToMatchExactly(marker)))
            {
                // Paranoia: check that the parsed hash and version match too.
                if (!StringComparer.OrdinalIgnoreCase.Equals(stash.VersionMarker.CommitHash, marker.CommitHash)) continue;
                if (!SemanticVersion.EqualityComparer.Equals(stash.VersionMarker.SemanticVersion, marker.SemanticVersion)) continue;
                yield return stash;
            }
        }

        /// <summary>
        /// Find all stashes matching the provided commit hash.
        /// </summary>
        private IEnumerable<IStash> FindByHash(VersionMarker marker)
        {
            if (string.IsNullOrWhiteSpace(marker.CommitHash)) yield break;
            foreach (var stash in EnumerateStashesMatching(new DirectoryNameVersionMarkerMapping().GetPatternToMatchCommitHash(marker)))
            {
                // Paranoia: check that the parsed hash matches too.
                if (!StringComparer.OrdinalIgnoreCase.Equals(stash.VersionMarker.CommitHash, marker.CommitHash)) continue;
                yield return stash;
            }
        }

        /// <summary>
        /// Find all stashes recorded by version number only (no hash) which match the specified semantic version's major.minor.
        /// </summary>
        private IEnumerable<IStash> FindByVersion(VersionMarker marker)
        {
            if (marker.SemanticVersion == null) yield break;
            foreach (var stash in EnumerateStashesMatching(new DirectoryNameVersionMarkerMapping().GetPatternToMatchSemanticVersion(marker)))
            {
                // Paranoia: check that the parsed version matches too.
                if (!SemanticVersion.EqualityComparer.Equals(stash.VersionMarker.SemanticVersion, marker.SemanticVersion)) continue;
                yield return stash;
            }
        }

        private IEnumerable<IStash> EnumerateStashesMatching(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(pattern));
            var root = EnsureRootPathExists();
            foreach (var match in root.EnumerateDirectories(pattern, SearchOption.TopDirectoryOnly))
            {
                var parsed = new DirectoryNameVersionMarkerMapping().MapFromDirectoryName(match.Name);
                if (parsed == null) continue;
                yield return GetStashWithCheckedName(parsed.Value, match.Name);
            }
        }

        private IStash GetStashWithCheckedName(VersionMarker marker, string directoryName)
        {
            var roundtripped = new DirectoryNameVersionMarkerMapping().MapToDirectoryName(marker);
            if (!StringComparer.OrdinalIgnoreCase.Equals(roundtripped, directoryName))
            {
                log.Warn($"Directory name {directoryName} does not match serialised marker: {roundtripped}");
            }

            var path = Path.Combine(rootPath, directoryName);
            var tempPath = Path.Combine(StashTemporaryDirectory, directoryName, Path.GetRandomFileName());
            var lockPath = Path.Combine(StashTemporaryDirectory, $"{directoryName}.lock");
            return new LocalStash(path, tempPath, lockPath, marker) { LocalFileSystem = LocalFileSystem };
        }

        public async IAsyncEnumerable<VersionMarker> List()
        {
            var list = new List<VersionMarker>();
            foreach (var stash in EnumerateStashesMatching("*"))
            {
                list.Add(stash.VersionMarker);
            }
            foreach (var version in list.OrderByDescending(v => v.SemanticVersion, SemanticVersion.MajorMinorBuildComparer))
            {
                yield return version;
            }
        }

        public async Task<VersionMarker[]> List(SemanticVersion majorMinor)
        {
            var versions = new List<VersionMarker>();
            foreach (var stash in EnumerateStashesMatching($"*_{majorMinor.Major}.{majorMinor.Minor}.*"))
            {
                // Paranoia: check that the parsed versions match too.
                if (!StringComparer.OrdinalIgnoreCase.Equals(stash.VersionMarker.SemanticVersion?.Major, majorMinor.Major)) continue;
                if (!StringComparer.OrdinalIgnoreCase.Equals(stash.VersionMarker.SemanticVersion?.Minor, majorMinor.Minor)) continue;
                versions.Add(stash.VersionMarker);
            }
            return versions.OrderByDescending(v => v.SemanticVersion, SemanticVersion.MajorMinorBuildComparer).ToArray();
        }

        public async Task Delete(VersionMarker marker)
        {
            var existing = await TryGet(marker);
            if (existing == null) return;
            // If it's not an exact match, ignore it.
            if (!VersionMarker.EqualityComparer.Equals(existing.VersionMarker, marker)) return;

            if (existing is LocalStash localStash)
            {
                await localStash.Delete();
            }
            else
            {
                Debug.Fail($"Not a {nameof(LocalStash)}? {existing.GetType()}");
                throw new ApplicationException($"Expected {nameof(LocalStash)}, found {existing.GetType()}");
            }
        }

        public async IAsyncEnumerable<string> CleanUpTemporaryObjects([EnumeratorCancellation] CancellationToken token)
        {
            await foreach (var path in LocalFileSystem.EnumerateAbsolutePaths(StashTemporaryDirectory, false).WithCancellation(token))
            {
                if (await TryCleanUp(path))
                {
                    yield return path;
                }
            }
        }

        //[DebuggerNonUserCode]
        private async Task<bool> TryCleanUp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    // Lock file. Try to delete.
                    File.Delete(tempPath);
                    return true;
                }
                else if (Directory.Exists(tempPath))
                {
                    return await LocalFileSystem.TryDeleteTemporaryPath(tempPath);
                }
            }
            catch
            {
                // Ignore failures.
            }
            return false;
        }

        internal class DirectoryNameVersionMarkerMapping
        {
            public string GetPatternToMatchExactly(VersionMarker marker)
            {
                if (!marker.IsValid) throw new ArgumentException(nameof(marker));
                return $"{marker.CommitHash}_{marker.SemanticVersion}";
            }

            public string GetPatternToMatchCommitHash(VersionMarker marker)
            {
                if (!marker.IsValid) throw new ArgumentException(nameof(marker));
                return $"{marker.CommitHash}_*";
            }

            public string GetPatternToMatchSemanticVersion(VersionMarker marker)
            {
                if (!marker.IsValid) throw new ArgumentException(nameof(marker));
                return $"*_{marker.SemanticVersion}";
            }

            public string MapToDirectoryName(VersionMarker marker)
            {
                return VersionMarkerStringConverter.ForDirectoryName().ToString(marker);
            }

            public const string UnknownPart = "unknown";

            public VersionMarker? MapFromDirectoryName(string name)
            {
                if (VersionMarkerStringConverter.ForDirectoryName().TryParse(name, out var marker))
                {
                    return marker;
                }
                return null;
            }
        }
    }
}
