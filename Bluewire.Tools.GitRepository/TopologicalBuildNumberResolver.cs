using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.GitRepository
{
    /// <summary>
    /// On the first-parent ancestry chain between two commits, locate a commit with the specified topological build number.
    /// </summary>
    public class TopologicalBuildNumberResolver
    {
        private readonly GitSession session;
        private readonly TopologicalBuildNumberCalculator calculator;

        public ISearcher SearchImplementation { get; set; } = new BinarySearcherImpl();

        public TopologicalBuildNumberResolver(GitSession session, TopologicalBuildNumberCalculator calculator = null)
        {
            this.session = session;
            this.calculator = calculator ?? new TopologicalBuildNumberCalculator(session);
        }

        public async Task<Ref> FindCommit(IGitFilesystemContext workingCopyOrRepo, Ref startRef, Ref endRef, int buildNumber)
        {
            if (workingCopyOrRepo == null) throw new ArgumentNullException(nameof(workingCopyOrRepo));
            if (startRef == null) throw new ArgumentNullException(nameof(startRef));
            if (endRef == null) throw new ArgumentNullException(nameof(endRef));
            if (buildNumber < 0) throw new BuildNumberOutOfRangeException($"Build number below zero is not permitted: {buildNumber}");

            var start = await session.ResolveRef(workingCopyOrRepo, startRef);
#if DEBUG
            var startNumber = await calculator.GetBuildNumber(workingCopyOrRepo, start, start);
            Debug.Assert(startNumber == 0);

#endif
            if (buildNumber == 0) return start;

            var end = await session.ResolveRef(workingCopyOrRepo, endRef);
            var endNumber = await calculator.GetBuildNumber(workingCopyOrRepo, start, end);
            if (buildNumber == endNumber) return end;
            if (buildNumber > endNumber) throw new BuildNumberOutOfRangeException($"Build number {buildNumber} exceeds the last known build number: {endNumber}");

            var candidatesDescending = await session.ListCommitsBetween(workingCopyOrRepo, start, end, new ListCommitsOptions { FirstParentOnly = true });

            var target = await SearchImplementation.Search(candidatesDescending, buildNumber, r => calculator.GetBuildNumber(workingCopyOrRepo, start, r));
            if (target != null) return target;
            throw new BuildNumberNotFoundException($"Build number {buildNumber} was not found between {start} and {end}. Does it live on a branch?");
        }

        public class LinearSearcherImpl : ISearcher
        {
            public async Task<Ref> Search(Ref[] candidatesDescending, int buildNumber, Func<Ref, Task<int?>> getBuildNumber)
            {
                foreach (var candidate in candidatesDescending)
                {
                    var current = await getBuildNumber(candidate);
                    if (current == buildNumber) return candidate;
                }
                return null;
            }
        }

        public class BinarySearcherImpl : ISearcher
        {
            public async Task<Ref> Search(Ref[] candidatesDescending, int buildNumber, Func<Ref, Task<int?>> getBuildNumber)
            {
                var candidates = candidatesDescending.Reverse().ToArray();
                var min = 0;
                var max = candidates.Length - 1;
                while (min <= max)
                {
                    var mid = (min + max) / 2;
                    var midRef = candidates[mid];
                    var midBuildNumber = await getBuildNumber(midRef);

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

        public interface ISearcher
        {
            Task<Ref> Search(Ref[] candidatesDescending, int buildNumber, Func<Ref, Task<int?>> getBuildNumber);
        }
    }
}
