using System;
using System.Collections.Generic;
using Bluewire.Conventions;

namespace Bluewire.Stash
{
    public readonly struct ResolvedVersionMarker
    {
        private sealed class SemanticVersionCommitHashEqualityComparer : IEqualityComparer<ResolvedVersionMarker>
        {
            public bool Equals(ResolvedVersionMarker x, ResolvedVersionMarker y)
            {
                return SemanticVersion.EqualityComparer.Equals(x.SemanticVersion, y.SemanticVersion) &&
                       string.Equals(x.CommitHash, y.CommitHash, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(ResolvedVersionMarker obj)
            {
                unchecked
                {
                    return (SemanticVersion.EqualityComparer.GetHashCode(obj.SemanticVersion) * 397) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(obj.CommitHash);
                }
            }
        }

        public static IEqualityComparer<ResolvedVersionMarker> EqualityComparer { get; } = new SemanticVersionCommitHashEqualityComparer();

        public ResolvedVersionMarker(SemanticVersion semanticVersion, string commitHash)
        {
            SemanticVersion = semanticVersion ?? throw new ArgumentNullException(nameof(semanticVersion));
            CommitHash = commitHash ?? throw new ArgumentNullException(nameof(commitHash));
        }

        public SemanticVersion SemanticVersion { get; }
        public string CommitHash { get; }

        public bool IsValid => !default(ResolvedVersionMarker).Equals(this);

        public static implicit operator VersionMarker(ResolvedVersionMarker source) => new VersionMarker(source.SemanticVersion, source.CommitHash);
    }
}
