using System;
using Bluewire.Conventions;

namespace Bluewire.Stash
{
    public readonly struct VersionMarkerStringConverter
    {
        private readonly char separatorCharacter;

        public VersionMarkerStringConverter(char separatorCharacter)
        {
            this.separatorCharacter = separatorCharacter;
        }

        public static VersionMarkerStringConverter ForDirectoryName() => new VersionMarkerStringConverter('_');
        public static VersionMarkerStringConverter ForIdentifierRoundtrip() => new VersionMarkerStringConverter(':');

        public const string UnknownPart = "unknown";
        public string ToString(VersionMarker marker)
        {
            if (!marker.IsValid) throw new ArgumentException("Marker is empty.", nameof(marker));
            var hashPart = marker.CommitHash ?? UnknownPart;
            var versionPart = marker.SemanticVersion?.ToString() ?? UnknownPart;
            return string.Concat(hashPart, separatorCharacter, versionPart);
        }

        public bool TryParse(string value, out VersionMarker marker)
        {
            marker = default;
            var separatorIndex = value.IndexOf(separatorCharacter);
            if (separatorIndex < 0) return false;

            var hashPart = value.Substring(0, separatorIndex);
            var versionPart = value.Substring(separatorIndex + 1);
            if (versionPart.Contains("_")) return false; // Not a recognised name. Might belong to a newer version?

            if (versionPart == UnknownPart)
            {
                if (hashPart == UnknownPart) return false;   // Both parts unknown?!
                marker = new VersionMarker(hashPart);
                return true;
            }
            if (!SemanticVersion.TryParse(versionPart, out var semanticVersion))
            {
                return false;
            }
            if (hashPart == UnknownPart)
            {
                marker = new VersionMarker(semanticVersion);
                return true;
            }

            marker = new VersionMarker(semanticVersion, hashPart);
            return true;
        }
    }
}
