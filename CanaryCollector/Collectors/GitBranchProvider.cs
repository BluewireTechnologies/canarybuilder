using System.Linq;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper;
using Bluewire.Common.GitWrapper.Model;

namespace CanaryCollector.Collectors
{
    public class GitRemoteBranchProvider : IBranchProvider
    {
        private readonly GitSession session;
        private readonly GitRepository repository;

        public GitRemoteBranchProvider(GitSession session, GitRepository repository)
        {
            this.session = session;
            this.repository = repository;
        }

        public async Task<string[]> GetUnmergedBranches(string mergeTarget)
        {
            var branches = await session.ListBranches(repository, new ListBranchesOptions { Remote = true, UnmergedWith = new Ref(mergeTarget) });
            return branches.Select(b => b.ToString()).ToArray();
        }
    }
}
