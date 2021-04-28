using System;
using System.Linq;

namespace Bluewire.Conventions
{
    public static class SprintNumber
    {
        /// <summary>
        /// Expects a two-part version number with no whitespace.
        /// </summary>
        /// <returns>Null if it's not a sprint number.</returns>
        public static Version Parse(string str)
        {
            if (str.Cast<char>().Any(Char.IsWhiteSpace)) return null;
            Version version;
            if (!Version.TryParse(str, out version)) return null;
            // Check that we only got the first two components:
            if (version.Revision < 0 && version.Build < 0) return version;
            return null;
        }
    }
}
