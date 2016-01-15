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
            if (!lines.Any()) throw new UnexpectedGitOutputFormatException(process.CommandLine, "No output.");
            if (lines.Count() > 1) throw new UnexpectedGitOutputFormatException(process.CommandLine, new UnexpectedGitOutputFormatDetails { Line = lines[1], Explanations = { $"{lines.Count() - 1} excess lines." } });
            if (String.IsNullOrWhiteSpace(lines.Single())) throw new UnexpectedGitOutputFormatException(process.CommandLine, "Empty output.");
            return lines.Single();
        }

        public static async Task ExpectSuccess(IConsoleProcess process)
        {
            var code = await process.Completed;
            if (code != 0) throw new GitException(process.CommandLine, code, String.Join(Environment.NewLine, await process.StdErr.ToStringAsync()));
        }
    }
}