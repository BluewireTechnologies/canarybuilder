using System.Collections.Generic;
using System.IO;

namespace Bluewire.Common.GitWrapper
{
    public class UnexpectedGitOutputFormatDetails
    {
        public string Line { get; set; }
        public ICollection<string> Explanations { get; } = new List<string>();

        public void Explain(TextWriter writer)
        {
            writer.WriteLine($"> {Line}");
            foreach (var explanation in Explanations)
            {
                writer.WriteLine($"    {explanation}");
            }
        }
    }
}
