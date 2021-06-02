using System;
using System.Globalization;
using Bluewire.Conventions;
using McMaster.Extensions.CommandLineUtils.Abstractions;

namespace Bluewire.Stash.Tool
{
    public class SemanticVersionValueParser : IValueParser<SemanticVersion?>
    {
        object? IValueParser.Parse(string? argName, string? value, CultureInfo culture)
        {
            return Parse(argName, value, culture);
        }

        public SemanticVersion? Parse(string? argName, string? value, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value)) return null;
            return SemanticVersion.FromString(value);
        }

        public Type TargetType => typeof(SemanticVersion);
    }
}
