using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CanaryCollector.Collectors
{
    public interface IBranchProvider
    {
        Task<string[]> GetUnmergedBranches(string mergeTarget);
    }
}
