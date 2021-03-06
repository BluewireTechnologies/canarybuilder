﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
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

            if (job.FinalTag != null && await session.TagExists(workingCopy, job.FinalTag)) throw new OutputRefAlreadyExistsException($"An existing ref conflicts with the requested tag: {job.FinalTag}");
            if (job.FinalBranch != null && await session.BranchExists(workingCopy, job.FinalBranch)) throw new OutputRefAlreadyExistsException($"An existing ref conflicts with the requested branch: {job.FinalBranch}");

            var temporaryBranch = job.TemporaryBranch ?? new Ref($"temp/canary/{Path.GetRandomFileName()}");
            try
            {
                logger.Info($"Starting at {job.Base}");
                // Checkout base ref.
                await session.CheckoutCompletelyClean(workingCopy, job.Base);
                // Run verifier against it.
                await VerifyBase(workingCopy, job, logger);
                // Check that the working copy is still clean.
                await AssertCleanWorkingCopy(session, workingCopy);

                logger.Info($"Using temporary branch {temporaryBranch}");
                // Create temporary branch from base ref and checkout.
                await session.CreateBranchAndCheckout(workingCopy, temporaryBranch);

                var results = new List<CanaryResult>();

                // Iterate through merges:
                foreach (var merge in job.Merges)
                {
                    var outcome = await TryMerge(session, workingCopy, merge, logger);
                    results.Add(new CanaryResult { Ref = merge.Ref, Outcome = outcome });
                }

                logger.Info($"{results.Count(r => r.Succeeded)} merged of {results.Count}, based on {job.Base}");
                var succeededRefs = results.Where(r => r.Succeeded).Select(r => r.Ref).ToList();
                foreach (var merge in job.Merges)
                {
                    if (succeededRefs.Contains(merge.Ref))
                    {
                        logger.Info($" +++  {merge.Ref}");
                    }
                    else
                    {
                        logger.Warn($" ---  {merge.Ref}");
                    }
                }

                // Tag, if requested.
                if (job.FinalTag != null)
                {
                    logger.Info($"Tagging the result as {job.FinalTag}");
                    await session.CreateAnnotatedTag(workingCopy, job.FinalTag, temporaryBranch, FormatTagMessage(results));
                }
                // Create final branch if requested.
                if (job.FinalBranch != null)
                {
                    logger.Info($"Branching the result as {job.FinalBranch}");
                    await session.CreateBranch(workingCopy, job.FinalBranch, temporaryBranch);
                }
            }
            finally
            {
                // Checkout starting branch, ensuring that working copy is left clean.
                await session.CheckoutCompletelyClean(workingCopy, startingBranch);

                await AssertCleanWorkingCopy(session, workingCopy);
            }
            // Delete temporary branch if we exited normally.
            if (await session.BranchExists(workingCopy, temporaryBranch))
            {
                await session.DeleteBranch(workingCopy, temporaryBranch, true);
            }
        }

        private string FormatTagMessage(List<CanaryResult> results)
        {
            var succeeded = results.Where(r => r.Succeeded).ToList();
            var failed = results.Where(r => !r.Succeeded).ToList();

            var writer = new StringWriter();
            writer.Write("CanaryBuilder: ");
            writer.WriteLine(succeeded.Any() ? $"{succeeded.Count} merged of {results.Count}" : "no merges");
            writer.WriteLine();
            writer.WriteLine("Branches incorporated:");
            writer.WriteLine(FormatResults(succeeded, s => s.Ref));
            writer.WriteLine("Branches rejected:");
            writer.WriteLine(FormatResults(failed, FormatFailureWithReason));
            return writer.ToString();
        }

        private string FormatResults(IList<CanaryResult> results, Func<CanaryResult, string> format)
        {
            if (!results.Any()) return "(none)";
            return String.Concat(results.Select(s => $"* {format(s)}{Environment.NewLine}").ToArray());
        }

        private string FormatFailureWithReason(CanaryResult result)
        {
            switch (result.Outcome)
            {
                case Outcome.Unknown:
                    return $"{result.Ref} (unknown failure)";
                case Outcome.MergeFailed:
                    return $"{result.Ref} (failed merge)";
                case Outcome.VerificationFailed:
                    return $"{result.Ref} (failed verification)";
                default:
                    return result.Ref;
            }
        }

        private static async Task VerifyBase(GitWorkingCopy workingCopy, MergeJobDefinition job, IJobLogger logger)
        {
            if (job.Verifier == null) return;
            using (logger.EnterScope("Verifying working copy"))
            {
                await job.Verifier.Verify(workingCopy, logger);
            }
        }

        /// <summary>
        /// Attempt to cleanly merge the specified candidate into HEAD.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="workingCopy"></param>
        /// <param name="candidate"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        private async Task<Outcome> TryMerge(GitSession session, GitWorkingCopy workingCopy, MergeCandidate candidate, IJobLogger logger)
        {
            var startingRef = await session.ResolveRef(workingCopy, Ref.Head);
            logger.Info($"Attempting to merge {candidate.Ref}");
            try
            {
                // * Try merge. If fails, abort this one and continue with next.
                await session.Merge(workingCopy, new MergeOptions { FastForward = MergeFastForward.Never }, candidate.Ref);
                logger.Info($"Cleanly merged {candidate.Ref}");
                await AssertCleanWorkingCopy(session, workingCopy); // Sanity check only.
            }
            catch (Exception ex)
            {
                if (workingCopy.IsMerging)
                {
                    logger.Warn($"Unable to cleanly merge {candidate.Ref}. It will be rolled back.", ex);
                    await session.AbortMerge(workingCopy);
                }
                else
                {
                    logger.Warn($"Unable to merge {candidate.Ref}.");
                }
                await session.ResetCompletelyClean(workingCopy, startingRef);
                return Outcome.MergeFailed;
            }

            try
            {
                logger.Info($"Verifying merge of {candidate.Ref}");
                // * Apply verifier. If fails, undo merge and continue with next.
                await VerifyMerge(workingCopy, candidate, logger);

                await AssertCleanWorkingCopy(session, workingCopy);
            }
            catch (Exception ex)
            {
                logger.Warn($"Post-merge verification of {candidate.Ref} failed. It will be rolled back.", ex);
                await session.ResetCompletelyClean(workingCopy, startingRef);
                return Outcome.VerificationFailed;
            }
            logger.Info($"Successfully incorporated {candidate.Ref}");
            return Outcome.Succeeded;
        }

        private static async Task VerifyMerge(GitWorkingCopy workingCopy, MergeCandidate candidate, IJobLogger logger)
        {
            if (candidate.Verifier == null) return;
            using (logger.EnterScope("Verifying working copy"))
            {
                await candidate.Verifier.Verify(workingCopy, logger);
            }
        }

        private static async Task AssertCleanWorkingCopy(GitSession session, GitWorkingCopy workingCopy)
        {
            if (!await session.IsClean(workingCopy))
            {
                throw new UncleanWorkingCopyException();
            }
        }

        class CanaryResult
        {
            public Ref Ref { get; set; }
            public Outcome Outcome { get; set; }
            public bool Succeeded => Outcome == Outcome.Succeeded;
        }

        enum Outcome
        {
            Unknown,
            Succeeded,
            MergeFailed,
            VerificationFailed
        }
    }
}
