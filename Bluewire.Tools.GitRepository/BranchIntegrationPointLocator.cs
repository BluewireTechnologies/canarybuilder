using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.GitRepository
{
    /// <summary>
    /// On the first-parent ancestry chain between two commits, locate the integration point of a third commit.
    /// </summary>
    public class BranchIntegrationPointLocator
    {
        private readonly GitSession session;

        public BranchIntegrationPointLocator(GitSession session)
        {
            this.session = session;
        }

        public async Task<Ref> FindCommit(IGitFilesystemContext workingCopyOrRepo, Ref startRef, Ref endRef, Ref subjectRef)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));
            if (startRef == null) throw new ArgumentNullException(nameof(startRef));
            if (endRef == null) throw new ArgumentNullException(nameof(endRef));
            if (subjectRef == null) throw new ArgumentNullException(nameof(subjectRef));

            var start = await session.ResolveRef(workingCopyOrRepo, startRef);
            var end = await session.ResolveRef(workingCopyOrRepo, endRef);
            var subject = await session.ResolveRef(workingCopyOrRepo, subjectRef);

            if (!await session.IsAncestor(workingCopyOrRepo, subject, end)) throw new CommitNotInAncestryChainException($"Ref {subjectRef} does not exist in the ancestry of {endRef}");
            if (Equals(end, subject)) return end;
            if (await session.IsAncestor(workingCopyOrRepo, subject, start)) return start;

            var firstParentChain = await session.ListCommitsBetween(workingCopyOrRepo, start, end, new ListCommitsOptions { FirstParentOnly = true });
            Debug.Assert(Equals(firstParentChain[0], end));

            var firstParents = new HashSet<Ref>(firstParentChain);
            if (firstParents.Contains(subject)) return subject;

            var subjectChain = await session.ListCommitsBetween(workingCopyOrRepo, subject, end, new ListCommitsOptions { AncestryPathOnly = true });
            Debug.Assert(Equals(subjectChain[0], end));

            var firstParentSubjectAncestry = subjectChain.Where(firstParents.Contains);
            var earliestMerge = firstParentSubjectAncestry.LastOrDefault();
            if (earliestMerge == null) throw new Exception("Ancestry chains have no matching elements.");
            return earliestMerge;
        }
    }
}
