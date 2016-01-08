using System;
using System.IO;
using System.Threading.Tasks;
using Bluewire.Common.Git;
using Bluewire.Common.Git.Model;
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
            // Record starting branch.
            var startingBranch = await session.GetCurrentBranch(workingCopy);
            // Assert working copy is clean.
            if (!await session.IsClean(workingCopy)) throw new InvalidOperationException("Working copy is not clean.");

            var temporaryBranch = job.TemporaryBranch ?? new Ref($"temp/canary/{Path.GetRandomFileName()}");
            try
            {
                // Checkout base ref.
                await session.Checkout(workingCopy, job.Base);
                // Run verifier against it.
                VerifyBase(workingCopy, job, logger);
                // Create temporary branch from base ref and checkout.
                await session.CreateBranchAndCheckout(workingCopy, temporaryBranch.ToString());

                // Iterate through merges:
                foreach (var merge in job.Merges)
                {
                    // * Try merge. If fails, abort this one and continue with next.

                    // * Apply verifier. If fails, undo merge and continue with next.
                    // VerifyMerge(workingCopy, merge, logger);
                }
                // Tag, if requested.
                // Create final branch if requested.
                if (job.FinalBranch != null) await session.CreateBranch(workingCopy, job.FinalBranch.ToString(), temporaryBranch);
            }
            finally
            {
                // Checkout starting branch.
                await session.Checkout(workingCopy, startingBranch);
                // Delete temporary branch.
                if (await session.RefExists(workingCopy, temporaryBranch))
                {
                    await session.DeleteBranch(workingCopy, temporaryBranch);
                }
                // Ensure working copy is left clean.
                if (!await session.IsClean(workingCopy))
                {
                }
            }
        }

        private void VerifyBase(GitWorkingCopy workingCopy, MergeJobDefinition job, IJobLogger logger)
        {
        }

        private void VerifyMerge(GitWorkingCopy workingCopy, MergeCandidate candidate, IJobLogger logger)
        {
        }
    }
}
