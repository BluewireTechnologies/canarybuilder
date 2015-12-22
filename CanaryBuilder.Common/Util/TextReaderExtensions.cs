using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanaryBuilder.Common.Util
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
