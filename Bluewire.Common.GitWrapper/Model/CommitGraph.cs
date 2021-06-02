using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bluewire.Common.GitWrapper.Model
{
    public class CommitGraph : ICommitGraph
    {
        private readonly IDictionary<Ref, Ref[]> parentage = new Dictionary<Ref, Ref[]>();
        private readonly IDictionary<Ref, ISet<Ref>> ancestry = new Dictionary<Ref, ISet<Ref>>();
        private readonly Dictionary<Ref, Ref> refInternTable = new Dictionary<Ref, Ref>();
        private static readonly Ref[] emptyRefs = new Ref[0];

        private Ref Intern(Ref @ref)
        {
            if (refInternTable.TryGetValue(@ref, out var existing)) return existing;
            refInternTable.Add(@ref, @ref);
            return @ref;
        }

        private Ref[] Intern(Ref[] refs)
        {
            if (refs == null) return emptyRefs;
            for (var i = 0; i < refs.Length; i++)
            {
                refs[i] = Intern(refs[i]);
            }
            return refs;
        }

        public bool Add(Ref commit, Ref[] parents)
        {
            commit = Intern(commit);
            if (parentage.TryGetValue(commit, out var existing))
            {
                Debug.Assert(existing.SequenceEqual(parents ?? emptyRefs), "Expected matching parentage for a given commit.");
                return false;
            }
            else
            {
                parentage.Add(commit, Intern(parents));
                return true;
            }
        }

        public bool Contains(Ref commit) => parentage.ContainsKey(commit);

        public IEnumerable<Ref> Parents(Ref commit)
        {
            if (parentage.TryGetValue(commit, out var parents))
            {
                return parents.ToArray();
            }
            return emptyRefs;
        }

        private IEnumerable<Ref> ParentsInternal(Ref commit)
        {
            parentage.TryGetValue(commit, out var parents);
            return parents ?? emptyRefs;
        }

        public IEnumerable<Ref> Ancestors(Ref commit)
        {
            return MemoiseAncestors(commit).ToArray();
        }

        private ISet<Ref> MemoiseAncestors(Ref commit)
        {
            if (ancestry.TryGetValue(commit, out var existing))
            {
                return existing;
            }
            var accumulator = new HashSet<Ref>();
            foreach (var parent in ParentsInternal(commit))
            {
                if (!accumulator.Add(parent)) continue;
                accumulator.UnionWith(MemoiseAncestors(parent));
            }
            ancestry.Add(commit, accumulator);
            return accumulator;
        }

        public IEnumerable<Ref> FirstParentAncestry(Ref commit, Ref stop)
        {
            var current = commit;
            while (parentage.TryGetValue(current, out var parents) && parents.Any())
            {
                current = parents.First();
                if (Equals(current, stop)) yield break;
                yield return current;
            }
        }
    }
}
