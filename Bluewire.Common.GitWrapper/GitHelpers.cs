using System;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.Console.Client.Shell;

namespace Bluewire.Common.GitWrapper
{
    public static class GitHelpers
    {
        public static async Task<string> ExpectOneLine(IConsoleProcess process)
        {
            await ExpectSuccess(process);

            var lines = await process.StdOut.ReadAllLinesAsync();
            if(lines.Count() != 1) throw new UnexpectedGitOutputFormatException(process.CommandLine);
            if(String.IsNullOrWhiteSpace(lines.Single())) throw new UnexpectedGitOutputFormatException(process.CommandLine);
            return lines.Single();
        }

        public static async Task ExpectSuccess(IConsoleProcess process)
        {
            var code = await process.Completed;
            if (code != 0) throw new GitException(process.CommandLine, code, String.Join(Environment.NewLine, await process.StdErr.ToStringAsync()));
        }
    }
}