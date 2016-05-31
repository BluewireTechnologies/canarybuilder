using System.Collections.Generic;

namespace Bluewire.Common.GitWrapper
{
    public interface IGitLineOutputParser<T>
    {
        IEnumerable<UnexpectedGitOutputFormatDetails> Errors { get; }
        bool Parse(string line, out T entry);
        T ParseOrNull(string line);
    }
}
