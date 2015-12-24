using System.Collections.Generic;
using System.IO;

namespace CanaryBuilder.Parsers
{
    public static class TextReaderExtensions
    {
        public static IEnumerable<string> ReadAllLines(this TextReader reader)
        {
            var line = reader.ReadLine();
            while (line != null)
            {
                yield return line;
                line = reader.ReadLine();
            }
        }
    }
}
