using System;
using System.IO;
using System.Threading.Tasks;
using CanaryBuilder.Common.Git;

namespace CanaryBuilder
{
    public class Diagnostics
    {
        public async Task<int> Run(TextWriter writer)
        {
            var git = await new GitFinder().FromEnvironment();
            if (git == null)
            {
                writer.WriteLine("Unable to find git.exe in the current PATH.");
                return ExitCode.GitNotFound;
            }
            writer.WriteLine($"Using Git from: {git.GetExecutableFilePath()}");
            try
            {
                var versionString = await git.GetVersionString();
                writer.WriteLine($"Git version: {versionString}");
            }
            catch (GitException ex)
            {
                writer.WriteLine("Unable to determine Git version.");
                ex.Explain(writer);
                return ExitCode.ErrorDeterminingVersion;
            }
            return 0;
        }

        public static class ExitCode
        {
            public const int GitNotFound = 2;
            public const int ErrorDeterminingVersion = 3;
        }
    }
}