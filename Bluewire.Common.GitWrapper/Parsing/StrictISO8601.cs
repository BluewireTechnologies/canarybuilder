using System;
using Bluewire.Common.GitWrapper;

namespace RefCleaner.Collectors
{
    public class StrictISO8601
    {
        public const string Pattern = "yyyy-MM-ddTHH:mm:sszzzz";

        public static DateTimeOffset? TryParseExact(string str, UnexpectedGitOutputFormatDetails error = null)
        {
            DateTimeOffset datestamp;
            if (!DateTimeOffset.TryParseExact(str, Pattern, null, System.Globalization.DateTimeStyles.None, out datestamp))
            {
                error?.Explanations.Add($"Datestamp wasn't recognised as a strict-ISO date: {str}");
                return null;
            }
            return datestamp;
        }
    }
}
