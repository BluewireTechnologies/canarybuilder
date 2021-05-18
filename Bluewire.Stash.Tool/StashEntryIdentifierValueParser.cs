using System;
using System.Globalization;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace Bluewire.Stash.Tool
{
    public class VersionMarkerIdentifierValueParser : IValueParser<VersionMarker?>
    {
        object? IValueParser.Parse(string? argName, string? value, CultureInfo culture)
        {
            return Parse(argName, value, culture);
        }

        public VersionMarker? Parse(string? argName, string? value, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value)) return null;

            if (VersionMarkerStringConverter.ForIdentifierRoundtrip().TryParse(value!, out var versionMarker)) return versionMarker;
            throw new FormatException($"Not a valid identifier: {value}");
        }

        public Type TargetType => typeof(VersionMarker);
    }
}
