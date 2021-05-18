using System;
using System.Collections.Generic;
using Bluewire.Conventions;

namespace Bluewire.Stash
{
    public readonly struct VersionMarker
    {
        private sealed class SemanticVersionCommitHashEqualityComparer : IEqualityComparer<VersionMarker>
        {
            public bool Equals(VersionMarker x, VersionMarker y)
            {
                return SemanticVersion.EqualityComparer.Equals(x.SemanticVersion, y.SemanticVersion) && string.Equals(x.CommitHash, y.CommitHash, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(VersionMarker obj)
            {
                unchecked
                {
                    return ((obj.SemanticVersion != null ? SemanticVersion.EqualityComparer.GetHashCode(obj.SemanticVersion) : 0) * 397) ^ (obj.CommitHash != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CommitHash) : 0);
                }
            }
        }

        public static IEqualityComparer<VersionMarker> EqualityComparer { get; } = new SemanticVersionCommitHashEqualityComparer();

        public VersionMarker(SemanticVersion semanticVersion, string commitHash)
        {
            SemanticVersion = semanticVersion ?? throw new ArgumentNullException(nameof(semanticVersion));
            CommitHash = commitHash ?? throw new ArgumentNullException(nameof(commitHash));
        }

        public VersionMarker(SemanticVersion semanticVersion)
        {
            SemanticVersion = semanticVersion ?? throw new ArgumentNullException(nameof(semanticVersion));
            CommitHash = null;
        }

        public VersionMarker(string commitHash)
        {
            SemanticVersion = null;
            CommitHash = commitHash ?? throw new ArgumentNullException(nameof(commitHash));
        }

        public SemanticVersion? SemanticVersion { get; }
        public string? CommitHash { get; }
        public bool IsValid => !default(VersionMarker).Equals(this);
        public bool IsComplete => CommitHash != null && SemanticVersion != null;

        public ResolvedVersionMarker Checked => IsComplete ? new ResolvedVersionMarker(SemanticVersion!, CommitHash!) : throw new InvalidOperationException();

        public override string ToString()
        {
            if (!IsValid) return "(none)";
            if (CommitHash == null) return SemanticVersion!.ToString();
            if (SemanticVersion == null) return CommitHash!;
            return $"{CommitHash} ({SemanticVersion})";
        }
    }
}
