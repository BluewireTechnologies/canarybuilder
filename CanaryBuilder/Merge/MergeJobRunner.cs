using CanaryBuilder.Common.Git;
using CanaryBuilder.Runners;

namespace CanaryBuilder.Merge
{
    public class MergeJobRunner
    {
        private readonly Git git;

        public MergeJobRunner(Git git)
        {
            this.git = git;
        }

        public void Run(GitWorkingCopy workingCopy, MergeJobDefinition job, IJobLogger logger)
        {
            var currentBranch = git.GetCurrentBranch(workingCopy);
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
