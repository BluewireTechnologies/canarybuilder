using System.Collections.Generic;
using System.Threading.Tasks;
using CanaryCollector.Model;

namespace CanaryCollector
{
    public interface IBranchCollector
    {
        Task<IEnumerable<Branch>> CollectBranches();
    }
}
