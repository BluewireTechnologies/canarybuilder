using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CanaryBuilder.Common.Git.Model;
using CanaryBuilder.Common.Shell;

namespace CanaryBuilder.Common.Git
{
    public class GitSession
    {
        private readonly Git git;
        private readonly IConsoleInvocationLogger logger;
        
        public GitSession(Git git, IConsoleInvocationLogger logger = null)
        {
            this.git = git;
            this.logger = logger;
        }

        public async Task<Ref> GetCurrentBranch(GitWorkingCopy workingCopy)
        {
            var process = new CommandLine(git.GetExecutableFilePath(), "rev-parse", "--abbrev-ref", "HEAD").RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                var currentBranchName = await GitHelpers.ExpectOneLine(process);

                // Should maybe check if it's a builtin of any sort, rather than just HEAD?
                if (currentBranchName != "HEAD") return new Ref(currentBranchName);

                return await ResolveRef(workingCopy, new Ref(currentBranchName));
            }
        }

        public async Task<Ref> ResolveRef(GitWorkingCopy workingCopy, Ref @ref)
        {
            if (@ref == null) throw new ArgumentNullException(nameof(@ref));

            var process = new CommandLine(git.GetExecutableFilePath(), "rev-list", "-1", @ref.ToString()).RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                var refHash = await GitHelpers.ExpectOneLine(process);
                return new Ref(refHash);
            }
        }

        public async Task<bool> IsClean(GitWorkingCopy workingCopy)
        {
            var process = new CommandLine(git.GetExecutableFilePath(), "status", "--porcelain").RunFrom(workingCopy.Root);
            using (logger?.LogInvocation(process))
            {
                return !await process.StdOut.StopBuffering().Any().SingleOrDefaultAsync();
            }
        }
    }
}