using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.GitRepository
{
    public class TopologicalBuildNumberProvider
    {
        private readonly TopologyCache topology;

        public TopologicalBuildNumberProvider(GitSession session, IGitFilesystemContext workingCopyOrRepo) : this(new TopologyCache(session, workingCopyOrRepo))
        {
        }

        public TopologicalBuildNumberProvider(TopologyCache topology)
        {
            this.topology = topology;
        }

        public async Task<int?> GetBuildNumber(Ref baseRef, Ref subject)
        {
            var resolvedBaseRef = await topology.ResolveRef(baseRef);
            var resolvedSubject = await topology.ResolveRef(subject);
            await topology.LoadAncestryPaths(resolvedBaseRef, resolvedSubject);
            return GetBuildNumberInternal(resolvedBaseRef, resolvedSubject);
        }

        private int? GetBuildNumberInternal(Ref resolvedBaseRef, Ref resolvedSubject)
        {
            if (Equals(resolvedBaseRef, resolvedSubject)) return 0;
            var graph = topology.GetChildCommitsGraphFromResolved(resolvedBaseRef);
            if (!graph.Contains(resolvedSubject)) throw new ArgumentException($"Not present in graph: {resolvedSubject}", nameof(resolvedSubject));
            return graph.Ancestors(resolvedSubject).Where(graph.Contains).Except(new [] { resolvedBaseRef }).Count() + 1;
        }

        /// <summary>
        /// On the first-parent ancestry chain between endRef and the baseRef, locate a commit with the specified topological build number.
        /// </summary>
        public async Task<Ref> FindCommit(Ref baseRef, Ref endRef, int buildNumber)
        {
            if (baseRef == null) throw new ArgumentNullException(nameof(baseRef));
            if (endRef == null) throw new ArgumentNullException(nameof(endRef));
            if (buildNumber < 0) throw new BuildNumberOutOfRangeException($"Build number below zero is not permitted: {buildNumber}");

            var resolvedBaseRef = await topology.ResolveRef(baseRef);
            var resolvedEndRef = await topology.ResolveRef(endRef);

#if DEBUG
            await topology.LoadAncestryPaths(resolvedBaseRef, resolvedEndRef);
            var startNumber = GetBuildNumberInternal(resolvedBaseRef, resolvedBaseRef);
            Debug.Assert(startNumber == 0);

#endif
            if (buildNumber == 0) return resolvedBaseRef;

            await topology.LoadAncestryPaths(resolvedBaseRef, resolvedEndRef);

            var endNumber = GetBuildNumberInternal(resolvedBaseRef, resolvedEndRef);
            if (buildNumber == endNumber) return resolvedEndRef;
            if (buildNumber > endNumber) throw new BuildNumberOutOfRangeException($"Build number {buildNumber} exceeds the last known build number: {endNumber}");

            var graph = topology.GetChildCommitsGraphFromResolved(resolvedBaseRef);
            var candidatesDescending = graph.FirstParentAncestry(resolvedEndRef, resolvedBaseRef).ToArray();

            var target = new BinarySearcherImpl().Search(candidatesDescending, buildNumber, r => GetBuildNumberInternal(resolvedBaseRef, r));
            if (target != null) return target;
            throw new BuildNumberNotFoundException($"Build number {buildNumber} was not found between {resolvedBaseRef} and {resolvedEndRef}. Does it live on a branch?");
        }

        public class BinarySearcherImpl
        {
            public Ref Search(Ref[] candidatesDescending, int buildNumber, Func<Ref, int?> getBuildNumber)
            {
                var candidates = candidatesDescending.Reverse().ToArray();
                var min = 0;
                var max = candidates.Length - 1;
                while (min <= max)
                {
                    var mid = (min + max) / 2;
                    var midRef = candidates[mid];
                    var midBuildNumber = getBuildNumber(midRef);

                    if (midBuildNumber == buildNumber) return midRef;
                    if (midBuildNumber == null)
                    {
                        min++;
                    }
                    else if (midBuildNumber > buildNumber)
                    {
                        max = mid - 1;
                    }
                    else
                    {
                        min = mid + 1;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Returns true if 'probe' lies on the first-parent ancestry path from 'tip' to 'baseRef'.
        /// </summary>
        public async Task<bool> IsFirstParentAncestor(Ref baseRef, Ref probe, Ref tip)
        {
            var resolvedBaseRef = await topology.ResolveRef(baseRef);
            var resolvedProbe = await topology.ResolveRef(probe);
            var resolvedTip = await topology.ResolveRef(tip);
            await topology.LoadAncestryPaths(resolvedBaseRef, resolvedTip);
            var graph = topology.GetChildCommitsGraphFromResolved(resolvedBaseRef);
            return graph.FirstParentAncestry(resolvedTip, resolvedBaseRef).Contains(resolvedProbe);
        }
    }
}
