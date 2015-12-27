using System.Collections.Generic;
using System.IO;
using Bluewire.Common.Console.Client.Shell;

namespace Bluewire.Common.Git
{
    public class UnexpectedGitOutputFormatDetails
    {
        public string Line { get; set; }
        public ICollection<string> Explanations { get; } = new List<string>();

        public void Explain(TextWriter writer)
        {
            writer.WriteLine($"> {Line}");
            foreach(var explanation in Explanations)
            {
                writer.WriteLine($"    {explanation}");
            }
        }
    }
}