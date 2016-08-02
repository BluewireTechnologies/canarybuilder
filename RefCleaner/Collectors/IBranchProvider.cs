using System.Collections.Generic;
using System.Threading.Tasks;
using Bluewire.Common.GitWrapper.Model;

namespace RefCleaner.Collectors
{
    public interface IBranchProvider
    {
        Task<BranchDetails[]> GetAllBranches();
        Task<ICollection<Ref>> GetMergedBranches(Ref mergeTarget);
        Task<bool> BranchExists(Ref branch);
    }
}
