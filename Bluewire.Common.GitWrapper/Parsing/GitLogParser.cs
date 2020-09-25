﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Async;
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
        private readonly LogDiffType logDiffType;
        private readonly List<UnexpectedGitOutputFormatDetails> errors = new List<UnexpectedGitOutputFormatDetails>();

        public GitLogParser(LogDiffType logDiffType)
        {
            this.logDiffType = logDiffType;
        }

        public IEnumerable<UnexpectedGitOutputFormatDetails> Errors => errors;

        public async Task<LogEntry[]> Parse(IAsyncEnumerator<string> lines, CancellationToken token)
        {
            var entries = new List<LogEntry>();
            using (var reader = new GitLogReader(lines, logDiffType))
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
