using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.GitRepository
{
    public class TopologyCache
    {
        private readonly Dictionary<Ref, CommitGraph> graphs = new Dictionary<Ref, CommitGraph>();
        private readonly Dictionary<Ref, Ref> resolvedRefs = new Dictionary<Ref, Ref>();
        private readonly Dictionary<Ref, bool> existingBranchesCache = new Dictionary<Ref, bool>();
        private readonly Dictionary<Ref, bool> existingRefsCache = new Dictionary<Ref, bool>();
        private readonly RepositoryStructureInspector inspector;

        public GitSession Session { get; }
        public IGitFilesystemContext WorkingCopyOrRepo { get; }

        public TopologyCache(GitSession session, IGitFilesystemContext workingCopyOrRepo)
        {
            this.Session = session ?? throw new ArgumentNullException(nameof(session));
            this.WorkingCopyOrRepo = workingCopyOrRepo ?? throw new ArgumentNullException(nameof(workingCopyOrRepo));
            inspector = new RepositoryStructureInspector(Session);
        }

        public async Task<ICommitGraph> LoadAncestryPaths(Ref start, Ref end)
        {
            var resolvedStart = await ResolveRef(start);
            var resolvedEnd = await ResolveRef(end);

            var graph = GetGraphForBaseRef(resolvedStart);
            if (graph.Contains(resolvedEnd)) return graph;
            var newRefs = await Session.AddAncestry(WorkingCopyOrRepo, graph, new Difference(resolvedStart, resolvedEnd));
            newRefs.ExceptWith(existingRefsCache.Keys);
            foreach (var r in newRefs) existingRefsCache.Add(r, true);
            return graph;
        }

        public async Task<Ref> ResolveRef(Ref subject)
        {
            // Not strictly correct. A tag may be a SHA1, but still needs resolving.
            if (RefHelper.IsSha1Hash(subject)) return subject;

            if (!resolvedRefs.TryGetValue(subject, out var resolved))
            {
                resolved = await Session.ResolveRef(WorkingCopyOrRepo, subject);
                resolvedRefs.Add(subject, resolved);
            }
            return resolved;
        }

        private CommitGraph GetGraphForBaseRef(Ref resolvedBaseRef)
        {
            if (!graphs.TryGetValue(resolvedBaseRef, out var graph))
            {
                graph = new CommitGraph();
                graphs.Add(resolvedBaseRef, graph);
            }
            return graph;
        }

        public async Task<ICommitGraph> GetChildCommitsGraph(Ref baseRef)
        {
            var resolvedBaseRef = await ResolveRef(baseRef);
            return GetChildCommitsGraphFromResolved(resolvedBaseRef);
        }

        public ICommitGraph GetChildCommitsGraphFromResolved(Ref resolvedBaseRef) => graphs[resolvedBaseRef];

        public async Task<bool> BranchExists(Ref branch)
        {
            if (existingRefsCache.TryGetValue(branch, out var anyRef) && !anyRef) return false;

            if (!existingBranchesCache.TryGetValue(branch, out var exists))
            {
                exists = await Session.BranchExists(WorkingCopyOrRepo, branch);
                existingBranchesCache.Add(branch, exists);
            }
            return exists;
        }

        public async Task<bool> RefExists(Ref subject)
        {
            if (existingBranchesCache.TryGetValue(subject, out var branchRef) && branchRef) return true;

            if (!existingRefsCache.TryGetValue(subject, out var exists))
            {
                exists = await Session.RefExists(WorkingCopyOrRepo, subject);
                existingBranchesCache.Add(subject, exists);
            }
            return exists;
        }
    }
}
