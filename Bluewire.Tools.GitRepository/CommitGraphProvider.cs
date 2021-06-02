using System.Collections.Generic;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace Bluewire.Tools.GitRepository
{
    public class CommitGraphProvider
    {
        private readonly GitSession gitSession;

        public CommitGraphProvider(GitSession gitSession)
        {
            this.gitSession = gitSession;
        }

        /// <summary>
        /// Collects all commits reachable from 'tips', excluding those reachable from 'baseRef', where 'baseRef' lies on the ancestry path.
        /// </summary>
        public async Task<ICommitGraph> Collect(IGitFilesystemContext workingCopyOrRepo, Ref baseRef, params Ref[] tips)
        {
            var graph = new CommitGraph();
            foreach (var tip in tips)
            {
                await gitSession.AddAncestry(workingCopyOrRepo, graph, new Difference(baseRef, tip));
            }
            return graph;
        }
    }
}
