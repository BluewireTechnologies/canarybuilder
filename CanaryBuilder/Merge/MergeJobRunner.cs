using System.Threading.Tasks;
using Bluewire.Common.Git;
using CanaryBuilder.Logging;

namespace CanaryBuilder.Merge
{
    public class MergeJobRunner
    {
        private readonly Git git;

        public MergeJobRunner(Git git)
        {
            this.git = git;
        }

        public async Task Run(GitWorkingCopy workingCopy, MergeJobDefinition job, IJobLogger logger)
        {
            var session = new GitSession(git, logger);
            var currentBranch = await session.GetCurrentBranch(workingCopy);

            // Record starting branch.
            // Assert working copy is clean.
            // Run verifier against it.
            // Checkout base ref.
            // Create temporary branch.
            // Iterate through merges:
            //   * Try merge. If fails, abort this one and continue with next.
            //   * Apply verifier. If fails, undo merge and continue with next.
            // Tag, if requested.
            // Rename temporary branch to final name if requested, or delete otherwise.

            // finally:
            // Checkout starting branch.
            // Ensure working copy is left clean.
        }
    }
}
