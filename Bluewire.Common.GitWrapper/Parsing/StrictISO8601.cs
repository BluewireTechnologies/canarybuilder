using System;
using Bluewire.Common.GitWrapper;

namespace RefCleaner.Collectors
{
    public class StrictISO8601
    {
        public const string Pattern = "yyyy-MM-ddTHH:mm:sszzzz";

        private static readonly string[] patterns =
        {
            Pattern,
            "yyyy-MM-ddTHH:mm:ss'Z'",
        };

        public static DateTimeOffset? TryParseExact(string str, UnexpectedGitOutputFormatDetails error = null)
        {
            DateTimeOffset datestamp;
            if (!DateTimeOffset.TryParseExact(str, patterns, null, System.Globalization.DateTimeStyles.None, out datestamp))
            {
                error?.Explanations.Add($"Datestamp wasn't recognised as a strict-ISO date: {str}");
                return null;
            }
            return datestamp;
        }
    }
}
