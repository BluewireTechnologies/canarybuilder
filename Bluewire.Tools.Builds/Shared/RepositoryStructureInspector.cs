using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;
using Bluewire.Conventions;
using Bluewire.Tools.Builds.FindBuild;
using Bluewire.Tools.GitRepository;

namespace Bluewire.Tools.Builds.Shared
{
    public class RepositoryStructureInspector
    {
        private readonly GitSession gitSession;

        public RepositoryStructureInspector(GitSession gitSession)
        {
            if (gitSession == null) throw new ArgumentNullException(nameof(gitSession));
            this.gitSession = gitSession;
        }

        public async Task<string> GetActiveVersionNumber(Common.GitWrapper.GitRepository repository, Ref commit)
        {
            try
            {
                return await GetActiveVersionNumberUsingMarkerFile(repository, commit);
            }
            catch (GitException ex)
            {
                try
                {
                    return await GetActiveVersionNumberFromTagSearch(repository, commit);
                } catch { /* Discard this exception and throw the original exception instead. */ }
                throw new RepositoryStructureException($"Unable to determine the active version number for the commit. {ex.Message}");
            }
        }

        private async Task<string> GetActiveVersionNumberUsingMarkerFile(Common.GitWrapper.GitRepository repository, Ref commit)
        {
            var cmd = gitSession.CommandHelper.CreateCommand("show", $"{commit}:.current-version");
            var line = await gitSession.CommandHelper.RunSingleLineCommand(repository, cmd);
            var versionNumber = line.Trim();
            if (SprintNumber.Parse(versionNumber) != null) return versionNumber;
            throw new RepositoryStructureException($"Unable to determine the active version number for the commit. The .current-version could not be parsed: {versionNumber}");
        }

        private async Task<string> GetActiveVersionNumberFromTagSearch(Common.GitWrapper.GitRepository repository, Ref commit)
        {
            var cmd = gitSession.CommandHelper.CreateCommand("describe", "--first-parent", commit);
            var line = await gitSession.CommandHelper.RunSingleLineCommand(repository, cmd);
            var versionNumber = line.Trim().Split('-').FirstOrDefault();
            if (versionNumber != null)
            {
                if (SprintNumber.Parse(versionNumber) != null) return versionNumber;
            }
            throw new RepositoryStructureException($"Unable to determine the active version number for the commit. The commit description could not be parsed: {line}");
        }

        public async Task<TagDetails> ResolveBaseTagForVersion(Common.GitWrapper.GitRepository repository, string versionNumber)
        {
            try
            {
                return await gitSession.ReadTagDetails(repository, new Ref(versionNumber));
            }
            catch (GitException ex)
            {
                throw new RepositoryStructureException($"Unable to locate the base tag for version number {versionNumber}. {ex.Message}");
            }
        }

        public async Task<TagDetails> ResolveBaseTagForCommit(Common.GitWrapper.GitRepository repository, Ref commit)
        {
            var versionNumber = await GetActiveVersionNumber(repository, commit);
            return await ResolveBaseTagForVersion(repository, versionNumber);
        }

        public async Task<Ref> ResolveTagOrTipOfBranchForVersion(IGitFilesystemContext repository, SemanticVersion semanticVersion)
        {
            var branchSemantics = new BranchSemantics();
            var endLocalBranchNames = branchSemantics.GetVersionLatestBranchNames(semanticVersion);
            foreach (var endLocalBranchName in endLocalBranchNames)
            {
                var endRef = await GetEndRef(endLocalBranchName);
                if (await gitSession.RefExists(repository, endRef)) return endRef;
            }
            return null;

            async Task<Ref> GetEndRef(string endLocalBranchName)
            {
                if (endLocalBranchName == "master")
                {
                    // This is only applicable for versions which predate the use of backport/* branches, where the
                    // beta version terminates at maint/*.
                    var maintTag = new Ref($"tags/maint/{semanticVersion.Major}.{semanticVersion.Minor}");
                    if (await gitSession.TagExists(repository, maintTag))
                    {
                        return RefHelper.GetRemoteRef(new Ref(maintTag));
                    }
                }
                return RefHelper.GetRemoteRef(new Ref(endLocalBranchName));
            }
        }

        public async Task<IntegrationQueryResult[]> QueryIntegrationPoints(Common.GitWrapper.GitRepository repository, Ref subject, StructuredBranch[] targetBranches)
        {
            var baseTag = await ResolveBaseTagForCommit(repository, subject);

            var integrationPoints = new List<IntegrationQueryResult>();
            var integrationPointLocator = new BranchIntegrationPointLocator(gitSession);
            foreach (var branch in targetBranches)
            {
                integrationPoints.Add(new IntegrationQueryResult {
                    Subject = subject,
                    TargetBranch = branch,
                    IntegrationPoint = await integrationPointLocator.FindCommit(repository, baseTag.ResolvedRef, new Ref(branch.ToString()), subject)
                });
            }
            return integrationPoints.ToArray();
        }

        /// <summary>
        /// Find all branches of the specified types which contain the given commit.
        /// If more than pruneThreshold branches are found, those which diverged from master when it contained the given commit
        /// will be pruned.
        /// </summary>
        /// <remarks>
        /// If pruning takes place, the integration point with master should be unique.
        /// If pruning does not take place, it is possible that some branches may share an integration point with master.
        /// By default, pruning will always occur if more than one branch is found.
        /// </remarks>
        public async Task<StructuredBranch[]> FindContainingBranches(Common.GitWrapper.GitRepository repository, BranchType[] types, Ref hash, int pruneThreshold = 1)
        {
            // Optimisation: only have Git explore the branches we care about.
            var branchFilter = new BranchSemantics().GetRemoteBranchFilters(types.Concat(new [] { BranchType.Master }).ToArray());

            var containingBranches = await gitSession.ListBranches(repository, new ListBranchesOptions { Contains = hash, Remote = true, BranchFilter = branchFilter });

            StructuredBranch? masterBranch;
            var recognisedBranches = ParseRecognisedBranches(types, containingBranches, out masterBranch);
            if (recognisedBranches.Length <= 1) return recognisedBranches;  // Only a single branch found.
            if (masterBranch == null) return recognisedBranches;            // Not merged to master yet.

            // Multiple branches.
            // Commits contained in master may be in a very large number of other branches via the master branch.
            // Detecting integration points for every one of these branches may take quite a while, but we can
            // quickly cull a lot of junk by finding branches which contain the integration into master and
            // excluding them.
            // But finding branches which contain a commit is also very expensive, and sometimes it's cheaper to
            // just brute-force the integration points after all. Therefore we only try to trim branches if we
            // found more than 'pruneThreshold' branches.
            if (recognisedBranches.Length <= pruneThreshold) return recognisedBranches;

            var commonAncestor = await ResolveBaseTagForCommit(repository, hash);

            var integrationPointLocator = new BranchIntegrationPointLocator(gitSession);
            var integrationPoint = await integrationPointLocator.FindCommit(repository, commonAncestor.ResolvedRef, new Ref(masterBranch.ToString()), hash);

            var branchesInheritingFromMaster = await gitSession.ListBranches(repository, new ListBranchesOptions { Contains = integrationPoint, Remote = true });

            StructuredBranch? recognisedMasterBranch;
            var excessBranches = ParseRecognisedBranches(types, branchesInheritingFromMaster, out recognisedMasterBranch);
            Debug.Assert(recognisedMasterBranch != null);
            return recognisedBranches.Except(excessBranches).Concat(new [] { masterBranch.Value }).ToArray();
        }

        private static StructuredBranch[] ParseRecognisedBranches(BranchType[] types, Ref[] branches, out StructuredBranch? masterBranch)
        {
            masterBranch = null;
            var parsedBranches = new List<StructuredBranch>();
            foreach (var branch in branches)
            {
                StructuredBranch parsed;
                if (!StructuredBranch.TryParse(branch, out parsed)) continue;

                var type = new BranchSemantics().GetBranchType(parsed);

                // Always retain master if present.
                if (BranchType.Master.Equals(type))
                {
                    Debug.Assert(masterBranch == null);
                    masterBranch = parsed;
                    parsedBranches.Add(parsed);
                }
                else if (types.Contains(type))
                {
                    if (SprintNumber.Parse(parsed.Name) == null) continue;
                    parsedBranches.Add(parsed);
                }
            }
            return parsedBranches.ToArray();
        }
    }
}
