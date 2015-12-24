using System;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;

namespace Bluewire.Common.Git
{
    public static class GitHelpers
    {
        public static async Task<string> ExpectOneLine(IConsoleProcess process)
        {
            var code = await process.Completed;
            if (code != 0) throw new GitException(process.CommandLine, code, String.Join(Environment.NewLine, await process.StdErr.ToStringAsync()));

            var lines = await process.StdOut.ReadAllLinesAsync();
            if(lines.Count() != 1) throw new UnexpectedGitOutputFormatException(process.CommandLine);
            if(String.IsNullOrWhiteSpace(lines.Single())) throw new UnexpectedGitOutputFormatException(process.CommandLine);
            return lines.Single();
        }
    }
}