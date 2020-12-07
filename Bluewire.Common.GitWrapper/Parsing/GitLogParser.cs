using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Common.GitWrapper.Parsing.Log;

namespace Bluewire.Common.GitWrapper.Parsing
{
    /// <summary>
    /// Parser for the output of 'git log'.
    /// </summary>
    /// <remarks>
    /// Instances of this class are not threadsafe.
    /// </remarks>
    public class GitLogParser : IGitAsyncOutputParser<LogEntry[]>
    {
        private readonly List<UnexpectedGitOutputFormatDetails> errors = new List<UnexpectedGitOutputFormatDetails>();
        public IEnumerable<UnexpectedGitOutputFormatDetails> Errors => errors;

        public async Task<LogEntry[]> Parse(IAsyncEnumerator<string> lines, CancellationToken token)
        {
            var entries = new List<LogEntry>();
            await using (var reader = new GitLogReader(lines))
            {
                while (await reader.NextLogEntry())
                {
                    if (reader.Current != null) entries.Add(reader.Current);
                }
                errors.AddRange(reader.Errors);
            }
            return entries.ToArray();
        }
    }
}
