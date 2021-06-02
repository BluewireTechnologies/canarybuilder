using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.Builds.Shared;

namespace Bluewire.Tools.Builds.FindBuild
{
    public class ResolveBuildVersionsFromTicketIdentifier : IBuildVersionResolutionJob
    {
        private readonly string ticketIdentifier;

        public ResolveBuildVersionsFromTicketIdentifier(string ticketIdentifier)
        {
            this.ticketIdentifier = ticketIdentifier;
        }

        public async Task<SemanticVersion[]> ResolveBuildVersions(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {
            var hashes = await ResolveToHashes(session, workingCopyOrRepo);

            // Tracing target branches can be expensive, so we should try to cull the hash list first.
            // If A is an ancestor of B, then the set of branches containing A must be a superset of the set of
            // branches containing B (since all branches containing B must contain A as well).
            // Unfortunately --is-ancestor is rather expensive too, especially for an O(n^2) operation, but it
            // beats the alternative.

            var basemostHashes = hashes.ToList();
            foreach (var hash in hashes)
            {
                if (await IsChildOfAny(session, workingCopyOrRepo, basemostHashes.ToArray(), hash))
                {
                    basemostHashes.Remove(hash);
                }
            }

            var resolver = new TargetBranchResolver(session, workingCopyOrRepo);
            var finder = new BuildVersionFinder(session, workingCopyOrRepo);

            var buildVersions = new List<SemanticVersion>();
            foreach (var hash in basemostHashes)
            {
                var targetBranches = await resolver.IdentifyTargetBranchesOfCommit(hash);
                buildVersions.AddRange(await finder.GetBuildVersionsFromCommit(hash, targetBranches));
            }
            return buildVersions.Distinct().ToArray();
        }

        private async Task<Ref[]> ResolveToHashes(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {
            var branchFilter = $"*{ticketIdentifier}*"; // Assume that ticket identifiers don't contain wildcard characters.
            var pattern = $"\\b{Regex.Escape(ticketIdentifier)}\\b";

            var commitsReferencingTickets = await session.ReadLog(workingCopyOrRepo, new LogOptions { MatchMessage = new Regex(pattern), IncludeAllRefs = true });
            var branchNamesReferencingTickets = await session.ListBranches(workingCopyOrRepo, new ListBranchesOptions { BranchFilter = new[] { branchFilter }, Remote = true });
            var branchTipsReferencingTickets = await ResolveAllRefs(session, workingCopyOrRepo, branchNamesReferencingTickets);

            var uniqueHashes = commitsReferencingTickets.Select(l => l.Ref).Concat(branchTipsReferencingTickets).Distinct().ToArray();
            if (uniqueHashes.Length == 0) throw new NoCommitsReferenceTicketIdentifierException(ticketIdentifier);
            return uniqueHashes;
        }

        private async Task<Ref[]> ResolveAllRefs(GitSession session, IGitFilesystemContext workingCopyOrRepo, Ref[] refNames)
        {
            var list = new List<Ref>();
            foreach (var refName in refNames)
            {
                list.Add(await session.ResolveRef(workingCopyOrRepo, refName));
            }
            return list.ToArray();
        }

        private async Task<bool> IsChildOfAny(GitSession session, IGitFilesystemContext workingCopyOrRepo, Ref[] hashes, Ref subject)
        {
            foreach (var hash in hashes)
            {
                if (subject == hash) continue;
                if (await session.IsAncestor(workingCopyOrRepo, hash, subject)) return true;
            }
            return false;
        }
    }
}
