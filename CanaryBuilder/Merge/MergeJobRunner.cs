﻿using System;
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
            
            if (job.FinalTag != null && await session.RefExists(workingCopy, job.FinalTag)) throw new OutputRefAlreadyExistsException($"An existing ref conflicts with the requested tag: {job.FinalTag}");
            if (job.FinalBranch != null && await session.RefExists(workingCopy, job.FinalBranch)) throw new OutputRefAlreadyExistsException($"An existing ref conflicts with the requested branch: {job.FinalBranch}");
            
            var temporaryBranch = job.TemporaryBranch ?? new Ref($"temp/canary/{Path.GetRandomFileName()}");
            try
            {
                // Checkout base ref.
                await session.CheckoutCompletelyClean(workingCopy, job.Base);
                // Run verifier against it.
                VerifyBase(workingCopy, job, logger);
                // Create temporary branch from base ref and checkout.
                await session.CreateBranchAndCheckout(workingCopy, temporaryBranch);

                var successful = 0;

                // Iterate through merges:
                foreach (var merge in job.Merges)
                {
                    if (await TryMerge(session, workingCopy, merge, logger)) successful++;

                    await AssertCleanWorkingCopy(session, workingCopy);
                }
                // Tag, if requested.
                if (job.FinalTag != null) await session.CreateAnnotatedTag(workingCopy, job.FinalTag, temporaryBranch, $"CanaryBuilder: {successful} merged of {job.Merges.Count}");
                // Create final branch if requested.
                if (job.FinalBranch != null) await session.CreateBranch(workingCopy, job.FinalBranch, temporaryBranch);
            }
            finally
            {
                // Checkout starting branch, ensuring that working copy is left clean.
                await session.CheckoutCompletelyClean(workingCopy, startingBranch);
                
                await AssertCleanWorkingCopy(session, workingCopy);
            }
            // Delete temporary branch if we exited normally.
            if (await session.RefExists(workingCopy, temporaryBranch))
            {
                await session.DeleteBranch(workingCopy, temporaryBranch, true);
            }
        }

        private void VerifyBase(GitWorkingCopy workingCopy, MergeJobDefinition job, IJobLogger logger)
        {
        }

        /// <summary>
        /// Attempt to cleanly merge the specified candidate into HEAD.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="workingCopy"></param>
        /// <param name="candidate"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private async Task<bool> TryMerge(GitSession session, GitWorkingCopy workingCopy, MergeCandidate candidate, IJobLogger logger)
        {
            try
            {
                // * Try merge. If fails, abort this one and continue with next.
                await session.Merge(workingCopy, new MergeOptions { FastForward = MergeFastForward.Never }, candidate.Ref);
            }
            catch
            {
                if(workingCopy.IsMerging) await session.AbortMerge(workingCopy);
                return false;
            }

            try
            {
                // * Apply verifier. If fails, undo merge and continue with next.
                VerifyMerge(workingCopy, candidate, logger);
            }
            catch
            {
                await session.ResetCompletelyClean(workingCopy, Ref.Head.Parent());
                return false;
            }
            return true;
        }

        private void VerifyMerge(GitWorkingCopy workingCopy, MergeCandidate candidate, IJobLogger logger)
        {
        }

        private static async Task AssertCleanWorkingCopy(GitSession session, GitWorkingCopy workingCopy)
        {
            if (!await session.IsClean(workingCopy))
            {
                throw new UncleanWorkingCopyException();
            }
        }
    }
}
